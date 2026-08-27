# 🧠 Đặc Tả Thuật Toán Thị Giác Máy Tính & Xử Lý Cử Chỉ

Tài liệu này tổng hợp toàn bộ cơ sở toán học, giải thuật trích xuất đặc trưng hình ảnh, mô hình điểm mốc xương khớp và thuật toán phân loại cử chỉ vận động được áp dụng trong **HAU Active**.

---

## 📌 1. Bảng Điểm Mốc Giải Phẫu (Landmark Topologies)

### 1.1. MediaPipe Pose (33 3D Keypoints)

Mô hình **MediaPipe BlazePose** phát hiện 33 điểm mốc xương khớp trong không gian 3 chiều:

<div align="center">
  <img src="assets/mediapipe_33_landmarks.png" width="450" alt="MediaPipe 33 Keypoints" />
</div>

Các điểm mốc trọng yếu được hệ thống khai thác:
- `0`: Mũi (Nose)
- `11, 12`: Khớp vai trái và vai phải (Left & Right Shoulder)
- `13, 14`: Khớp khuỷu tay trái và khuỷu tay phải (Left & Right Elbow)
- `15, 16`: Khớp cổ tay trái và cổ tay phải (Left & Right Wrist)
- `19, 20`: Khớp ngón trỏ trái và ngón trỏ phải (Left & Right Index)
- `23, 24`: Khớp hông trái và hông phải (Left & Right Hip)

---

## 📐 2. Thuật toán Tính toán Trọng tâm Cơ thể (Body Center)

Để đạt được sự ổn định cao nhất và tránh hiện tượng dao động do vung tay hoặc nhấc chân khi chạy tại chỗ, vị trí trọng tâm cơ thể $C(C_x, C_y)$ được tính bằng trung bình cộng tọa độ của 4 khớp thân trên cốt lõi (2 vai và 2 hông):

$$C_x = \frac{x_{11} + x_{12} + x_{23} + x_{24}}{4} \times W$$

$$C_y = \frac{y_{11} + y_{12} + y_{23} + y_{24}}{4} \times H$$

*Trong đó:*
- $x_i, y_i \in [0, 1]$: Tọa độ chuẩn hóa của điểm mốc thứ $i$.
- $W, H$: Chiều rộng và chiều cao của khung hình video (ví dụ: $640 \times 480$).

---

## 🔒 3. Cơ chế Khóa & Cân chỉnh Ngưỡng Động (Dynamic Thresholds)

### 3.1. Cử chỉ Khóa (Clap Gesture Detection)

Để bắt đầu chạy hoặc hiệu chỉnh lại vị trí mà không cần chạm vào bàn phím, người chơi thực hiện động tác chắp hai tay hoặc vỗ hai cổ tay lại gần nhau trước ngực.

Hệ thống tính khoảng cách Euclid chuẩn hóa $d$ giữa hai cổ tay (Landmarks 19 & 20):

$$d = \sqrt{(x_{19} - x_{20})^2 + (y_{19} - y_{20})^2}$$

**Điều kiện kích hoạt Khóa (`Locked`):**
$$\begin{cases}
d < \text{threshold\_clap} \quad (0.05) \\
\text{clap\_confirm\_count} \ge \text{clap\_confirm\_frames} \quad (3\text{ frames}) \\
\text{clap\_cooldown} = 0
\end{cases}$$

Sau khi thỏa mãn, biến `bool_locked` chuyển sang `True`, thiết lập lại tọa độ gốc và gán `clap_cooldown = 30` khung hình ($\approx 1\text{s}$) để chống lặp lệnh.

---

### 3.2. Thiết lập Biên Giới hạn Động (Dynamic Boundary Calculation)

Tại thời điểm khóa thành công, kích thước hình học của thân người được ghi nhận:
- Chiều rộng vai: $W_{\text{torso}} = |x_{11} - x_{12}| \times W$
- Chiều cao thân: $H_{\text{torso}} = |y_{24} - y_{12}| \times H$

Các đường ngưỡng giới hạn ảo được tính toán tỷ lệ thuận theo cơ thể:

$$L_{\text{fixed}} = x_{11} \cdot W + W_{\text{torso}} \cdot \text{threshold\_horizontal}$$

$$R_{\text{fixed}} = x_{12} \cdot W - W_{\text{torso}} \cdot \text{threshold\_horizontal}$$

$$U_{\text{fixed}} = y_{12} \cdot H - H_{\text{torso}} \cdot |\text{threshold\_vertical}|$$

$$D_{\text{fixed}} = y_{24} \cdot H + H_{\text{torso}} \cdot |\text{threshold\_vertical}|$$

*Cấu hình tham số chuẩn (`config.py`):*
- $\text{threshold\_horizontal} = 0.0$ (Ngưỡng nhạy nghiêng người)
- $\text{threshold\_vertical} = -0.2$ (Ngưỡng nhạy bật nhảy / ngồi xổm)

---

### 3.3. Thuật toán Phân loại Hành động (Action State Classifier)

Trong mỗi khung hình tiếp theo, vị trí trọng tâm hiện tại $(C_x, C_y)$ được đối chiếu với vị trí khung hình trước $(C_{x,\text{prev}}, C_{y,\text{prev}})$ và các đường ngưỡng:

```python
# 1. Rẽ / Nghiêng người sang Phải (Move Right)
if center_x <= right_fixed and prev_frame_width > right_fixed:
    move = 1

# 2. Rẽ / Nghiêng người sang Trái (Move Left)
if center_x >= left_fixed and prev_frame_width < left_fixed:
    move = 2

# 3. Bật Nhảy (Jump)
if center_y <= upper_fixed and prev_frame_height > upper_fixed:
    move = 3

# 4. Ngồi Xổm / Trượt (Crouch / Slide)
if center_y >= lower_fixed and prev_frame_height < lower_fixed:
    move = 4
```

---

## 🖐️ 4. Thuật toán Điều Khiển Con Trỏ Bàn Tay (Hand Tracking)

### 4.1. Khử rung Trọng tâm Bàn tay (Hand Center Filtering)

Để con trỏ chuột không bị giật lag khi các đầu ngón tay cử động nhẹ, hệ thống lấy trung bình cộng của 3 điểm mốc bàn tay phải:
- Khớp gốc ngón trỏ (`INDEX_FINGER_MCP`)
- Khớp gốc ngón út (`PINKY_MCP`)
- Khớp cổ tay (`WRIST`)

$$H_x = \frac{x_{\text{index}} + x_{\text{pinky}} + x_{\text{wrist}}}{3} \times W$$

$$H_y = \frac{y_{\text{index}} + y_{\text{pinky}} + y_{\text{wrist}}}{3} \times H$$

Nếu trong khung hình bị mất dấu bàn tay do che khuất, hệ thống tự động giữ lại giá trị tọa độ của khung hình trước đó $(H_{x,\text{prev}}, H_{y,\text{prev}})$ nhằm chống hiện tượng con trỏ biến mất đột ngột.

---

### 4.2. Cơ chế Tương tác theo Thời gian dừng (Hover-to-Click)

Trong Unity (`CursorController.cs`), con trỏ bàn tay được trang bị một bộ đếm thời gian dừng (Dwell Timer):

$$\text{Progress} = \frac{\Delta t_{\text{hover}}}{T_{\text{target}}} \quad (T_{\text{target}} = 2.0\text{ giây})$$

- Khi con trỏ đi vào vùng va chạm của nút UI (`RectTransform`), $\Delta t_{\text{hover}}$ bắt đầu tăng và cập nhật hình tròn tiến trình (`Image.fillAmount`).
- Khi $\text{Progress} \ge 1.0$, sự kiện `Button.onClick.Invoke()` được kích hoạt tự động và phát âm thanh phản hồi `ClickSound`.
- Nếu con trỏ rời khỏi nút trước khi đủ $2.0\text{s}$, tiến trình được đặt lại về $0$.
