import cv2
import ctypes


def setup_window():
    """Setup OpenCV window with custom properties."""
    user32 = ctypes.windll.user32
    screen_width = user32.GetSystemMetrics(0)
    screen_height = user32.GetSystemMetrics(1)

    fixed_width = screen_width // 4
    fixed_height = screen_height // 4
    window_x = 0
    window_y = 0

    cv2.namedWindow("MediaPipe Pose", cv2.WINDOW_NORMAL)
    cv2.moveWindow("MediaPipe Pose", window_x, window_y)
    cv2.resizeWindow("MediaPipe Pose", fixed_width, fixed_height)

    cv2.setWindowProperty("MediaPipe Pose", cv2.WND_PROP_FULLSCREEN, cv2.WINDOW_NORMAL)
    cv2.setWindowProperty("MediaPipe Pose", cv2.WND_PROP_TOPMOST, 1)

    hwnd = ctypes.windll.user32.FindWindowW(None, "MediaPipe Pose")
    ctypes.windll.user32.SetWindowLongW(
        hwnd, -16, ctypes.windll.user32.GetWindowLongW(hwnd, -16) & ~0x00800000
    )
    ctypes.windll.user32.SetWindowLongW(
        hwnd, -20, ctypes.windll.user32.GetWindowLongW(hwnd, -20) | 0x80000 | 0x20
    )

    opacity = 255
    ctypes.windll.user32.SetLayeredWindowAttributes(hwnd, 0, opacity, 0x2)

    return fixed_width, fixed_height
