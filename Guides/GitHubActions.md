
### Docker Compose Build, Init, and Security Pipeline

This guide helps you integrate security scans and build steps into your GitHub Actions pipeline.

---

### 1. Trigger the Pipeline

The workflow triggers on:
```yaml
on:
  push:
    branches: [ "sql" ]
  pull_request:
    branches: [ "sql" ]
```
Every push or PR to the `sql` branch will run the pipeline.

---

### 2. Checkout Code

Checkout your repository first:
```yaml
- name: Checkout Code
  uses: actions/checkout@v3
```

---

### 3. Set Up .env File

Create environment variables needed at runtime:
```yaml
- name: Create .env file
  run: |
    echo "DB_SA_PASSWORD=${{ secrets.DB_SA_PASSWORD }}" >> .env
    echo "API_DB_CONNECTION_STRING=${{ secrets.API_DB_CONNECTION_STRING }}" >> .env
```

---

### 4. Secret Scanning with Gitleaks

Install and run Gitleaks:
```yaml
- name: Install Gitleaks
  run: |
    curl -sSL https://github.com/gitleaks/gitleaks/releases/download/v8.24.3/gitleaks_8.24.3_linux_x64.tar.gz -o gitleaks.tar.gz
    tar -xzf gitleaks.tar.gz
    chmod +x gitleaks
    sudo mv gitleaks /usr/local/bin/gitleaks

- name: Run Gitleaks Scan and Display Results
  run: |
    gitleaks detect --source . --no-git --report-format json --report-path gitleaks-report.json || true
    echo "================= Gitleaks Report ================="
    cat gitleaks-report.json | jq .
    echo "==================================================="
```
Purpose: Detect any hardcoded secrets or credentials.

---

### 5. Vulnerability Scanning with Trivy

Install and scan Docker images for vulnerabilities:
```yaml
- name: Install Trivy
  run: |
    sudo apt-get install wget apt-transport-https gnupg lsb-release -y
    wget -qO - https://aquasecurity.github.io/trivy-repo/deb/public.key | sudo apt-key add -
    echo deb https://aquasecurity.github.io/trivy-repo/deb $(lsb_release -sc) main | sudo tee -a /etc/apt/sources.list.d/trivy.list
    sudo apt-get update
    sudo apt-get install trivy -y

- name: Run Trivy vulnerability scan
  run: |
    trivy image mcr.microsoft.com/mssql/server:2022-latest
    trivy image mcr.microsoft.com/mssql-tools
    trivy image dockercompose-api || true
    trivy image dockercompose-client || true
```
Purpose: Find vulnerabilities in Docker images.

---

### 6. Lint Dockerfiles with Hadolint

Check Dockerfile best practices:
```yaml
- name: Install Hadolint
  run: |
    sudo wget -O /bin/hadolint https://github.com/hadolint/hadolint/releases/latest/download/hadolint-Linux-x86_64
    sudo chmod +x /bin/hadolint

- name: Lint API Dockerfile
  run: hadolint ./API/Dockerfile

- name: Lint Client Dockerfile
  run: hadolint ./Client/Dockerfile
```
Purpose: Improve Dockerfile quality and security.

---

### 7. Setup .NET

Prepare for build and analysis:
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'
```
Purpose: Install .NET SDK.

---

### 8. Restore Dependencies

Restore NuGet packages:
```yaml
- name: Restore API Dependencies
  working-directory: ./API
  run: dotnet restore

- name: Restore Client Dependencies
  working-directory: ./Client
  run: dotnet restore
```

---

### 9. Build and Analyze Code

Compile and verify code formatting:
```yaml
- name: .NET Build (API and Client)
  run: |
    dotnet build ./API --no-restore --configuration Release
    dotnet build ./Client --no-restore --configuration Release

- name: Run .NET Static Analysis (API)
  working-directory: ./API
  run: dotnet format --verify-no-changes --severity error

- name: Run .NET Static Analysis (Client)
  working-directory: ./Client
  run: dotnet format --verify-no-changes --severity error
```

---

### 10. Check .NET Package Vulnerabilities

```yaml
- name: Run .NET Dependency Vulnerability Scans (API)
  working-directory: ./API
  run: dotnet list package --vulnerable

- name: Run .NET Dependency Vulnerability Scans (Client)
  working-directory: ./Client
  run: dotnet list package --vulnerable
```
Purpose: Find outdated or insecure dependencies.

---

### 11. Run Docker Compose

Spin up all containers and test:
```yaml
- name: Run Docker Compose
  run: docker-compose -f docker-compose.yml up --build --abort-on-container-exit
```
Shutdown after build:
```yaml
- name: Shut down Docker Compose
  if: always()
  run: docker-compose -f docker-compose.yml down -v
```

---

### Final Tips
- Always run security and quality checks before merging.
- Fix any real leaks or vulnerabilities found.
- Understand that some Gitleaks warnings might be false positives (e.g., Markdown links).

