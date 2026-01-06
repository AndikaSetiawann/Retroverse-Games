# 🎮 RetroVerse Gaming

> **Informasi Mahasiswa**
> **Nama:** Andika Setiawan
> **NIM:** 312310470
> **Kelas:** TI.23.A5

---

## 📌 Gambaran Umum Proyek

**RetroVerse Gaming** adalah sebuah platform **e-commerce game** yang dirancang untuk penjualan game retro hingga modern secara digital. Aplikasi ini dibangun menggunakan **ASP.NET Core MVC (.NET 8.0)** dengan pendekatan arsitektur yang rapi dan terstruktur, sehingga mudah dikembangkan dan dikelola.

Platform ini menyediakan dua sisi utama, yaitu **pengguna (customer)** dan **administrator (admin)**. Pengguna dapat menjelajahi katalog game, melakukan pembelian, serta mengelola koleksi game yang telah dibeli. Sementara itu, admin memiliki dashboard khusus untuk mengelola data game, pesanan, dan pengguna.

---

## ✨ Fitur Utama

### 🛒 Fitur Pengguna (Customer)

* **Katalog Game Interaktif**
  Menampilkan daftar game lengkap dengan deskripsi, harga, dan visual menarik.

* **Keranjang Belanja Pintar**
  Menambahkan game ke keranjang, mengatur jumlah, serta proses checkout yang mudah.

* **Wishlist**
  Menyimpan game favorit untuk dibeli di lain waktu.

* **Library Digital**
  Game yang telah dibeli akan tersimpan otomatis di library pribadi pengguna.

* **Proses Checkout Aman**
  Alur pemesanan yang sederhana dan terstruktur.

---

### 🛡️ Fitur Admin Dashboard

* **Manajemen Game (CRUD)**
  Menambah, mengubah, dan menghapus data game.

* **Manajemen Pesanan**
  Melihat dan mengelola status pesanan pelanggan.

* **Manajemen Pengguna**
  Mengatur data pengguna dan hak akses (role).

* **Tools Developer**
  Fitur seed data dan pengelolaan gambar game langsung dari dashboard.

---

## 🛠️ Teknologi yang Digunakan

* **Framework**: ASP.NET Core MVC (.NET 8.0)
* **Database**: MongoDB (NoSQL)
* **Frontend / Styling**: Bootstrap & Custom CSS
* **Authentication**: ASP.NET Core Identity & Session-based Authentication

---

## 🚀 Cara Menjalankan Aplikasi

### 🔧 Prasyarat

Pastikan perangkat telah terinstal:

* [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)
* [MongoDB](https://www.mongodb.com/try/download/community) (local server atau MongoDB Atlas)

---

### ▶️ Langkah Menjalankan

1. **Clone repository**

   ```bash
   git clone https://github.com/AndikaSetiawann/Retroverse-Games.git
   ```

2. **Masuk ke direktori proyek**

   ```bash
   cd "Retroverse games"
   ```

3. **Konfigurasi Database**
   Ubah file `appsettings.json` dan sesuaikan **MongoDB Connection String**.

4. **Jalankan aplikasi**

   ```bash
   dotnet run
   ```

5. **Akses melalui browser**

   ```
   http://localhost:5000
   https://localhost:5001
   ```

---

## 📚 Tujuan Pengembangan

Proyek ini dikembangkan sebagai **Tugas Akhir Mata Kuliah Pemrograman Visual Semester 5**, dengan tujuan:

* Menerapkan konsep **ASP.NET Core MVC**
* Mengintegrasikan **database NoSQL (MongoDB)**
* Membangun sistem e-commerce yang realistis dan siap dikembangkan lebih lanjut

---

## 🏁 Penutup

**RetroVerse Gaming** diharapkan dapat menjadi dasar pengembangan platform game digital yang lebih kompleks di masa depan, dengan fitur yang scalable, aman, dan user-friendly.

---

✨ *Dikembangkan oleh Andika Setiawan – 2025*
