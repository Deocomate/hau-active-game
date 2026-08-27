# 🏗️ Kiến Trúc Hệ Thống HAU Active

Tài liệu này đặc tả chi tiết kiến trúc kỹ thuật, luồng truyền dữ liệu liên tiến trình, giao thức truyền thông TCP/UDP Socket và cơ chế đồng bộ hóa thời gian thực giữa **Python AI Server** và **Unity 3D Client**.

---

## 📐 1. Mô hình Phân lớp Hệ thống (System Architecture Layers)

Hệ thống được thiết kế theo mô hình **Client-Server phân tán cục bộ (Local Distributed Architecture)** nhằm phân tách triệt để giữa tác vụ xử lý thị giác máy tính nặng (AI Inference) và tác vụ kết xuất đồ họa tương tác (Game Rendering Loop).

```
+-------------------------------------------------------------------------+
|                              INPUT LAYER                                |
|   - 720p/1080p Standard RGB Webcam (Integrated or USB)                  |
|   - Real-time video frame capture (30 fps)                              |
+-------------------------------------------------------------------------+
                                    │ (BGR raw frames)
                                    ▼
+-------------------------------------------------------------------------+
|                     PYTHON COMPUTER VISION BACKEND                      |
|   - OpenCV: Frame Preprocessing, BGR -> RGB color conversion            |
|   - MediaPipe Pose (BlazePose): 33 3D body keypoints detection          |
|   - MediaPipe Hands: 21 hand landmarks & classification (Right Hand)    |
|   - Detection Logic: Body Center, Dynamic Thresholds, Clap Lock         |
|   - Socket Server (TCP Port 5052, UDP Port 5053)                        |
+-------------------------------------------------------------------------+
                                    │ (TCP String packet stream)
                                    ▼
+-------------------------------------------------------------------------+
|                       UNITY 3D GAMEPLAY FRONTEND                        |
|   - SocketClient.cs: Non-blocking socket listener & stream decoding     |
|   - CursorController.cs: Hand center mapping & Radial Hover-to-Click    |
|   - PlayerController.cs: 3-lane movement, Jump, Slide with collider mod |
|   - FloorManager.cs & TileManager.cs: Procedural generation & pooling   |
|   - Gameplay Scenes: Menu, Endless Run, Fruit Slicing, Wall Shape-Fit   |
+-------------------------------------------------------------------------+
                                    │ (Visual & Audio Feedback)
                                    ▼
+-------------------------------------------------------------------------+
|                             USER EXPERIENCE                             |
|   - 3D Visual Feedback at 60 FPS (Zero-touch Exergame experience)       |
+-------------------------------------------------------------------------+
```

---

## 🔌 2. Giao thức Socket & Cấu trúc Gói tin

### 2.1. Cấu hình Cổng Mạng (Network Configuration)

- **Địa chỉ Host:** `127.0.0.1` (Localhost loopback)
- **Cổng TCP (Port 5052):** Truyền tải dòng dữ liệu tọa độ thời gian thực liên tục từ Python sang Unity.
- **Cổng UDP (Port 5053):** Kênh phụ trợ truyền thông điệp điều khiển/sự kiện ngược từ Unity sang Python nếu cần.

### 2.2. Định dạng Gói tin (Data Packet Schema)

Gói tin được mã hóa dưới dạng chuỗi văn bản ASCII ngắn gọn để tối ưu hóa hiệu suất truyền nhận và phân tích cú pháp:

```text
"[Fullbody_Landmarks], (Hand_X, Hand_Y), (Center_X, Center_Y), Move_Code"
```

#### Phân rã các trường dữ liệu:

1. **`Fullbody_Landmarks` (Chuỗi JSON Array):**
   - Danh sách tọa độ $(x, y)$ của 9 điểm mốc thân trên quan trọng: `[0, 16, 14, 12, 11, 13, 15, 24, 23]` (Mũi, hai cổ tay, hai khuỷu tay, hai vai, hai hông).
   - Được sử dụng trực tiếp bởi chế độ **Tạo dáng qua tường (Hole in the Wall)** để so sánh độ khớp tư thế.

2. **`Hand_X, Hand_Y` (Cặp số nguyên):**
   - Tọa độ trọng tâm bàn tay phải sau khi tính trung bình cộng 3 điểm mốc (khớp gốc ngón trỏ, khớp gốc ngón út, cổ tay).
   - Được sử dụng bởi `CursorController.cs` và chế độ **Chém hoa quả (Fruit Slicing)**.

3. **`Center_X, Center_Y` (Cặp số nguyên):**
   - Tọa độ trọng tâm phần thân người (trung bình cộng 2 vai và 2 hông).

4. **`Move_Code` (Số nguyên từ 0 đến 5):**
   - Mã lệnh hành động rời rạc xác định trạng thái điều khiển tức thời của người chơi:
     - `0`: Không có chuyển động đặc biệt (Neutral/Idle).
     - `1`: Di chuyển / Nghiêng người sang Phải (Move Right).
     - `2`: Di chuyển / Nghiêng người sang Trái (Move Left).
     - `3`: Bật nhảy (Jump).
     - `4`: Ngồi xổm / Trượt người (Crouch/Slide).
     - `5`: Cử chỉ khóa / Vỗ tay trước ngực (Lock / Recalibrate).

---

## 🧵 3. Cơ chế Đa luồng & Quản lý Bộ nhớ (Threading & Memory Model)

### 3.1. Phía Python (`main.py` & `detection.py`)
- Khởi tạo `socket.socket(socket.AF_INET, socket.SOCK_STREAM)` ở chế độ blocking/streaming.
- Vòng lặp camera sử dụng OpenCV `cv2.VideoCapture` đọc khung hình tuần tự.
- Chuyển đổi không gian màu sang RGB và gọi `pose.process()` và `hands.process()`.
- Gửi toàn bộ dữ liệu qua hàm `conn.sendall()`.

### 3.2. Phía Unity (`SocketClient.cs`)
- Áp dụng mẫu thiết kế **Singleton** (`SocketClient.Instance`) và `DontDestroyOnLoad(gameObject)` để duy trì kết nối mạng xuyên suốt quá trình chuyển Scene (`Menu` $\rightarrow$ `Run` $\rightarrow$ `Fruit` $\rightarrow$ `Hole`).
- Trong hàm `Update()`, `networkStream.DataAvailable` được kiểm tra không khóa (Non-blocking) để tránh làm sụt giảm FPS của Game Loop.
- Dữ liệu thô được gán vào thuộc tính công khai `SocketClient.Instance.Data` để các script chức năng truy xuất tự do.

---

## 🔄 4. Vòng đời Trò chơi & Máy Trạng thái (Game State Machine)

```mermaid
stateDiagram-v2
    [*] --> MainMenu: Khởi động Game
    MainMenu --> ModeSelect: Hover nút Modes (2s)
    MainMenu --> Shop: Hover nút Shop (2s)
    MainMenu --> RunningScene: Hover nút Start (2s)
    
    state RunningScene {
        [*] --> Uncalibrated: Chờ cử chỉ khóa
        Uncalibrated --> ActiveRunning: Clap Gesture (Move_Code = 5)
        ActiveRunning --> ActiveRunning: Chạy 3 làn / Nhảy / Ngồi né vật cản
        ActiveRunning --> FruitScene: Va chạm Fruit Portal
        ActiveRunning --> HoleScene: Va chạm Hole Portal
        ActiveRunning --> GameOver: Va chạm Chướng ngại vật
    }
    
    state FruitScene {
        [*] --> SlicingLoop: Ném hoa quả & bom
        SlicingLoop --> RunningScene: Hết giờ / Chém trúng bom
    }
    
    state HoleScene {
        [*] --> WallFittingLoop: Tường tiến tới
        WallFittingLoop --> RunningScene: Hoàn thành / Va chạm tường
    }
    
    GameOver --> MainMenu: Hover nút Replay / Home
```
