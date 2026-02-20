# Workspace Index

Quick guide to navigate `D:\POC_NEW\POC`.

## Core Apps
- `EcommerceAPI/BulkyBook-POC`: .NET backend solution (`Ecommerce.sln`)
- `ecommerce-ui`: Angular frontend application

## Automation and CI
- `.github/workflows`: GitHub Actions workflows
- `scripts`: root PowerShell helper scripts
- `docker-compose.yml`: full stack local container orchestration

## Reference Material
- `README.md`: primary project documentation
- `E-Commerce_Project_Presentation.md`: project presentation
- `*_SUMMARY.md`, `*_GUIDE.md`, `*_CHECKLIST.md`: feature and implementation notes
- `Images`: image assets used in docs/demos

## Common Commands
```powershell
# Build backend
dotnet build EcommerceAPI/BulkyBook-POC/Ecommerce.sln

# Run backend API
dotnet run --project EcommerceAPI/BulkyBook-POC/Ecommerce.API

# Install and run frontend
cd ecommerce-ui
npm install
npm start
```

