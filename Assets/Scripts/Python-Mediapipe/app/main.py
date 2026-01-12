import cv2
import mediapipe as mp
import socket

from .config import HOST, PORT
from .window_manager import setup_window
from .detection import detection, DetectionState

mp_pose = mp.solutions.pose
mp_hands = mp.solutions.hands


def run():
    """Main entry point for the MediaPipe application."""
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        s.bind((HOST, PORT))
        s.listen()

        fixed_width, fixed_height = setup_window()
        cap = cv2.VideoCapture(0)

        with mp_pose.Pose(
            min_detection_confidence=0.5, min_tracking_confidence=0.5
        ) as pose, mp_hands.Hands(
            min_detection_confidence=0.5, min_tracking_confidence=0.5
        ) as hands:

            while True:  # Vòng lặp chính để chờ kết nối
                print(f"\n[MediaPipe] Server started on {HOST}:{PORT}")
                print(
                    "[MediaPipe] Waiting for Unity to connect... (Start Unity game now!)"
                )

                conn, addr = s.accept()
                print(f"[MediaPipe] Connected by {addr}")

                # Reset detection state for new connection
                state = DetectionState()

                try:
                    while cap.isOpened():
                        success, image = cap.read()
                        if not success:
                            print("Ignoring empty camera frame.")
                            break

                        image = cv2.resize(
                            image,
                            (fixed_width, fixed_height),
                            interpolation=cv2.INTER_LINEAR,
                        )
                        image = detection(image, pose, hands, conn, state)
                        image = cv2.flip(image, 1)

                        cv2.imshow("MediaPipe Pose", image)
                        if cv2.waitKey(1) & 0xFF == ord("q"):
                            cap.release()
                            cv2.destroyAllWindows()
                            return  # Thoát hoàn toàn khi nhấn 'q'

                except (
                    ConnectionResetError,
                    ConnectionAbortedError,
                    BrokenPipeError,
                ) as e:
                    print(f"\n[MediaPipe] Unity disconnected: {e}")
                    print("[MediaPipe] Returning to waiting mode...")
                    conn.close()
                    continue  # Quay lại chờ kết nối mới

        cap.release()
        cv2.destroyAllWindows()


if __name__ == "__main__":
    run()
