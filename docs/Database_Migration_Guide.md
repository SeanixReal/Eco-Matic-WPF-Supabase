# Eco-Matic Database Migration & Networking Guide

Since you are moving this project to another laptop and won't be using a cloud database, you have two options for the MySQL database: **transfer the database entirely** to the new laptop, or **host it locally on your current PC** and connect to it over Wi-Fi.

---

## Option 1: Move the Database to the Laptop (Offline / Standalone)
Use this option if you want the laptop to run completely on its own without needing this PC turned on.

**Step 1: Install MySQL on the laptop**
Install MySQL Server (version 8.0+) on your new laptop. Ensure your `root` password is set to `admin123` so the C# connection string doesn't break.

**Step 2: Import the Database**
1. Copy the `docs/ecomatic_db_dump.sql` file (which I just generated for you) to your laptop.
2. Open your MySQL Command Line, MySQL Workbench, or your terminal on the laptop.
3. Create the empty database:
   ```sql
   CREATE DATABASE ecomatic_db;
   ```
4. Import the data using terminal/command prompt:
   ```bash
   mysql -u root -padmin123 ecomatic_db < ecomatic_db_dump.sql
   ```
5. Your application will now work smoothly on the laptop since it's hardcoded to look for `127.0.0.1` (localhost).

---

## Option 2: Host the Database on this PC for the Laptop to Connect
Use this option if the PC and Laptop are on the exact same Wi-Fi network and you want them to share the live data.

**Step 1: Find this PC's IP Address**
1. Open Command Prompt and type `ipconfig`.
2. Find the `IPv4 Address` (it usually looks like `192.168.1.X`).

**Step 2: Change the C# Connection String**
1. Open `Eco-Matic/Data/MySqlStore.cs`.
2. Change `Server=127.0.0.1;` to `Server=192.168.1.X;` (use the IP from Step 1).

**Step 3: Allow External Connections in MySQL**
1. By default, MySQL only allows `localhost`. You need to allow the `root` user to connect from anywhere.
2. Run this inside your MySQL Server:
   ```sql
   CREATE USER 'root'@'%' IDENTIFIED BY 'admin123';
   GRANT ALL PRIVILEGES ON *.* TO 'root'@'%' WITH GRANT OPTION;
   FLUSH PRIVILEGES;
   ```
3. *(Windows Only)* Open your **Windows Defender Firewall** and create an **Inbound Rule** to allow TCP Port **3306**.

---

### Fixing the "Build Errors" via `dotnet run`
If you see an exit code `1` or `127` in the terminal when trying to run the app, it's typically because you were in the **Root Repository Folder**, which has *multiple* projects (`Eco-Matic` and `Eco-Matic-Console`). 

Whenever you need to run the application via CLI going forward, make sure your terminal is inside the `Eco-Matic` GUI project directory first:
```bash
cd Eco-Matic
dotnet run
```
