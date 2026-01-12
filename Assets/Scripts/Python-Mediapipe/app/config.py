# Configuration constants
HOST = "127.0.0.1"
PORT = 5052

# Thresholds
threshold_clap = (
    0.05  # Euclidean distance threshold (reduced from 0.08 for stricter detection)
)
threshold_horizontal = 0
threshold_vertical = -0.2

# Clap detection settings
clap_confirm_frames = 3  # Số frame liên tiếp cần xác nhận trước khi trigger clap
clap_cooldown_frames = 30  # Cooldown sau khi clap (~1 giây ở 30fps)

# Selected landmarks for full body detection
selected_landmarks = [0, 16, 14, 12, 11, 13, 15, 24, 23]
