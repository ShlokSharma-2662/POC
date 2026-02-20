# How to Run Resilience Tests

## ✅ Where to Run the Test Script

### ❌ DON'T Use:
- **Package Manager Console** (in Visual Studio) - This is for NuGet commands only
- **Command Prompt (cmd)** - Won't work (needs PowerShell)

### ✅ DO Use:
1. **PowerShell Terminal** (Recommended)
2. **Visual Studio Terminal** (not Package Manager Console)
3. **VS Code Terminal**
4. **Windows Terminal**

---

## 🚀 Step-by-Step Instructions

### Option 1: PowerShell (Recommended)

1. **Open PowerShell:**
   - Press `Win + X` → Select "Windows PowerShell" or "Terminal"
   - Or search "PowerShell" in Start menu

2. **Navigate to project directory:**
   ```powershell
   cd D:\POC_2025\POC\EcommerceAPI\BulkyBook-POC
   ```

3. **Run the test script:**
   ```powershell
   .\scripts\test-resilience.ps1 -TestOAuth
   ```

### Option 2: Visual Studio Terminal

1. **In Visual Studio:**
   - Go to: **View** → **Terminal** (or press `Ctrl + ` `)
   - This opens a PowerShell terminal (NOT Package Manager Console)

2. **Navigate to project:**
   ```powershell
   cd EcommerceAPI\BulkyBook-POC
   ```

3. **Run the script:**
   ```powershell
   .\scripts\test-resilience.ps1 -TestOAuth
   ```

### Option 3: VS Code Terminal

1. **In VS Code:**
   - Press `` Ctrl + ` `` to open terminal
   - Or: **Terminal** → **New Terminal**

2. **Make sure it's PowerShell:**
   - Check bottom-right corner
   - If it says "cmd", click it and select "PowerShell"

3. **Run the script:**
   ```powershell
   .\scripts\test-resilience.ps1 -TestOAuth
   ```

---

## 🔧 If You Get Execution Policy Error

If you see: `"execution of scripts is disabled on this system"`

**Fix it:**
```powershell
# Run PowerShell as Administrator, then:
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Then try running the script again.

---

## 📋 Quick Reference

### Test OAuth Only:
```powershell
.\scripts\test-resilience.ps1 -TestOAuth
```

### Test Email Only (requires token):
```powershell
.\scripts\test-resilience.ps1 -TestEmail -UserToken "your_token_here"
```

### Test Everything:
```powershell
.\scripts\test-resilience.ps1 -All
```

### Custom Base URL:
```powershell
.\scripts\test-resilience.ps1 -TestOAuth -BaseUrl "https://localhost:5001"
```

---

## 🎯 Visual Guide

### ✅ Correct Terminal (PowerShell):
```
PS D:\POC_2025\POC\EcommerceAPI\BulkyBook-POC> .\scripts\test-resilience.ps1 -TestOAuth
```

### ❌ Wrong Terminal (Package Manager Console):
```
PM> .\scripts\test-resilience.ps1 -TestOAuth
# This won't work!
```

---

## 💡 Pro Tip

**Before running tests, make sure:**
1. ✅ Application is running (`dotnet run`)
2. ✅ You're in the correct directory (`EcommerceAPI\BulkyBook-POC`)
3. ✅ Using PowerShell (not cmd or Package Manager Console)

---

## 🚨 Troubleshooting

### "Script cannot be loaded because running scripts is disabled"
**Solution:** Run PowerShell as Administrator and execute:
```powershell
Set-ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### "Cannot find path"
**Solution:** Make sure you're in the correct directory:
```powershell
cd D:\POC_2025\POC\EcommerceAPI\BulkyBook-POC
pwd  # Verify current directory
```

### "The term '.\scripts\test-resilience.ps1' is not recognized"
**Solution:** Use full path or check script exists:
```powershell
Test-Path .\scripts\test-resilience.ps1  # Should return True
```

---

**Remember:** Use **PowerShell Terminal**, NOT Package Manager Console! 🚀

