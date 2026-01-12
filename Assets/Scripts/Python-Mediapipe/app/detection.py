import cv2
import math
import mediapipe as mp
from .config import (
    threshold_clap,
    threshold_horizontal,
    threshold_vertical,
    selected_landmarks,
    clap_confirm_frames,
    clap_cooldown_frames,
)

mp_hands = mp.solutions.hands


class DetectionState:
    """Class to hold detection state variables."""

    def __init__(self):
        self.bool_locked = False
        self.prev_frame_width = 0
        self.prev_frame_height = 0
        self.left_fixed = 0
        self.right_fixed = 0
        self.upper_fixed = 0
        self.lower_fixed = 0
        self.prev_hand_x = 0
        self.prev_hand_y = 0
        self.clap_cooldown = 0  # Cooldown để tránh cập nhật liên tục khi clap
        self.clap_confirm_count = 0  # Đếm số frame liên tiếp thỏa mãn điều kiện clap
        # Lưu vị trí hand landmarks của frame trước để tính toán hybrid
        self.left_hand_pos = None  # (x, y) của tay trái từ Hand detection
        self.right_hand_pos = None  # (x, y) của tay phải từ Hand detection


def detection(image, pose, hands, conn, state):
    """
    Process image for pose and hand detection.

    Args:
        image: Input image frame
        pose: MediaPipe Pose instance
        hands: MediaPipe Hands instance
        conn: Socket connection to Unity
        state: DetectionState instance

    Returns:
        Processed image with annotations
    """
    height, width, _ = image.shape
    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

    fullbody_output = "[]"
    hand_output = "(0, 0)"
    pose_output = "()"
    move = 0

    # Pose detection
    pose_results = pose.process(image_rgb)
    if pose_results.pose_landmarks:
        center_x = int(
            (
                pose_results.pose_landmarks.landmark[11].x
                + pose_results.pose_landmarks.landmark[12].x
                + pose_results.pose_landmarks.landmark[23].x
                + pose_results.pose_landmarks.landmark[24].x
            )
            / 4
            * width
        )
        center_y = int(
            (
                pose_results.pose_landmarks.landmark[11].y
                + pose_results.pose_landmarks.landmark[12].y
                + pose_results.pose_landmarks.landmark[23].y
                + pose_results.pose_landmarks.landmark[24].y
            )
            / 4
            * height
        )
        cv2.circle(
            image, (center_x, center_y), radius=10, color=(0, 0, 255), thickness=-1
        )

        if state.prev_frame_width == 0 and state.prev_frame_height == 0:
            state.prev_frame_width = center_x
            state.prev_frame_height = center_y

        if not state.bool_locked:
            left = int(pose_results.pose_landmarks.landmark[11].x * width)
            right = int(pose_results.pose_landmarks.landmark[12].x * width)
            state.left_fixed = int(left + (left - right) * threshold_horizontal)
            state.right_fixed = int(right - (left - right) * threshold_horizontal)

            upper = int(pose_results.pose_landmarks.landmark[12].y * height)
            lower = int(pose_results.pose_landmarks.landmark[24].y * height)
            state.upper_fixed = int(upper - (lower - upper) * threshold_vertical)
            state.lower_fixed = int(lower + (lower - upper) * threshold_vertical)

        # === IMPROVED CLAP DETECTION ===
        # Sử dụng Pose landmarks 19 (left index), 20 (right index) cho wrist positions
        # Tính Euclidean distance 2D thay vì chỉ X-distance

        left_wrist_x = pose_results.pose_landmarks.landmark[19].x
        left_wrist_y = pose_results.pose_landmarks.landmark[19].y
        right_wrist_x = pose_results.pose_landmarks.landmark[20].x
        right_wrist_y = pose_results.pose_landmarks.landmark[20].y

        # Tính khoảng cách Euclidean normalized
        dx = left_wrist_x - right_wrist_x
        dy = left_wrist_y - right_wrist_y
        euclidean_distance = math.sqrt(dx * dx + dy * dy)

        # Giảm cooldown mỗi frame
        if state.clap_cooldown > 0:
            state.clap_cooldown -= 1

        # Kiểm tra điều kiện clap với Euclidean distance
        clap_detected = euclidean_distance < threshold_clap

        if clap_detected and state.clap_cooldown == 0:
            # Tăng confirm count
            state.clap_confirm_count += 1

            # Chỉ trigger clap khi đủ số frame xác nhận liên tiếp
            if state.clap_confirm_count >= clap_confirm_frames:
                # Cập nhật lại vị trí lock dựa trên vị trí hiện tại
                left = int(pose_results.pose_landmarks.landmark[11].x * width)
                right = int(pose_results.pose_landmarks.landmark[12].x * width)
                state.left_fixed = int(left + (left - right) * threshold_horizontal)
                state.right_fixed = int(right - (left - right) * threshold_horizontal)

                upper = int(pose_results.pose_landmarks.landmark[12].y * height)
                lower = int(pose_results.pose_landmarks.landmark[24].y * height)
                state.upper_fixed = int(upper - (lower - upper) * threshold_vertical)
                state.lower_fixed = int(lower + (lower - upper) * threshold_vertical)

                state.bool_locked = True
                state.clap_cooldown = clap_cooldown_frames  # Cooldown từ config
                state.clap_confirm_count = 0  # Reset confirm count

                # Reset prev_frame values to current center position to avoid wrong movement
                state.prev_frame_width = center_x
                state.prev_frame_height = center_y

                print(f"locked - position reset (distance: {euclidean_distance:.4f})")
                move = 5
        else:
            # Reset confirm count nếu không còn thỏa mãn điều kiện
            state.clap_confirm_count = 0

        if state.bool_locked:
            cv2.rectangle(
                image,
                (state.left_fixed, state.upper_fixed),
                (state.right_fixed, state.lower_fixed),
                (255, 0, 0),
                2,
            )

            if (
                center_x <= state.right_fixed
                and state.prev_frame_width > state.right_fixed
            ):
                print("right")
                move = 1
            if (
                center_x >= state.left_fixed
                and state.prev_frame_width < state.left_fixed
            ):
                print("left")
                move = 2
            if (
                center_x > state.right_fixed
                and state.prev_frame_width <= state.right_fixed
            ):
                print("left")
                move = 2
            if (
                center_x < state.left_fixed
                and state.prev_frame_width >= state.left_fixed
            ):
                print("right")
                move = 1
            if (
                center_y <= state.upper_fixed
                and state.prev_frame_height > state.upper_fixed
            ):
                print("jump")
                move = 3
            if (
                center_y >= state.lower_fixed
                and state.prev_frame_height < state.lower_fixed
            ):
                print("crouch")
                move = 4

        state.prev_frame_width = center_x
        state.prev_frame_height = center_y

        pose_output = f"({center_x}, {center_y}), {move}"

    # Hand detection
    hand_results = hands.process(image_rgb)
    if hand_results.multi_hand_landmarks:
        # Chỉ lấy tay phải (Right hand) để điều khiển
        for idx, hand_landmarks in enumerate(hand_results.multi_hand_landmarks):
            handedness = hand_results.multi_handedness[idx].classification[0].label
            if handedness != "Right":
                continue  # Bỏ qua nếu không phải tay phải

            hand_center_x = int(
                (
                    hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_MCP].x
                    + hand_landmarks.landmark[mp_hands.HandLandmark.PINKY_MCP].x
                    + hand_landmarks.landmark[mp_hands.HandLandmark.WRIST].x
                )
                / 3
                * width
            )
            hand_center_y = int(
                (
                    hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_MCP].y
                    + hand_landmarks.landmark[mp_hands.HandLandmark.PINKY_MCP].y
                    + hand_landmarks.landmark[mp_hands.HandLandmark.WRIST].y
                )
                / 3
                * height
            )
            cv2.circle(
                image,
                (hand_center_x, hand_center_y),
                radius=10,
                color=(0, 0, 255),
                thickness=-1,
            )  # Red dot for right hand only
            hand_output = f"({hand_center_x}, {hand_center_y})"
            # Update the previous hand coordinates
            state.prev_hand_x, state.prev_hand_y = hand_center_x, hand_center_y
            break
    else:
        # Use previous hand coordinates if no hand is detected
        hand_output = f"({state.prev_hand_x}, {state.prev_hand_y})"

    # Full body detection
    if pose_results.pose_landmarks:
        landmarks = []
        for idx in sorted(selected_landmarks, reverse=True):
            landmark = pose_results.pose_landmarks.landmark[idx]
            x = int(landmark.x * width)
            y = int(landmark.y * height)
            landmarks.append([x, y])
            cv2.circle(image, (x, y), radius=5, color=(0, 255, 0), thickness=-1)
        fullbody_output = f"{landmarks}"

    # Combine all outputs into one line
    combined_output = f"{fullbody_output}, {hand_output}, {pose_output}"
    conn.sendall(f"{fullbody_output}, {hand_output}, {pose_output}".encode())
    print(combined_output)

    return image
