
### Docker Compose Build, Init, and Security Pipeline (with Stages)

This guide explains the staged version of the pipeline.

### Stage 1: Checkout and Setup

```yaml
- name: Checkout Code
  uses: actions/checkout@v3

- name: Set up Docker Compose
  run: |
    sudo apt-get update
    sudo apt-get install docker-compose -y

- name: Create .env file
  run: |
    echo "DB_SA_PASSWORD=${{ secrets.DB_SA_PASSWORD }}" >> .env
    echo "API_DB_CONNECTION_STRING=${{ secrets.API_DB_CONNECTION_STRING }}" >> .env
```

### Stage 2: Security Scans

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

### Stage 3: Docker Image Security

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

### Stage 4: Dockerfile Linting

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

### Stage 5: Build and Static Code Analysis (.NET)

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0.x'

- name: Restore API Dependencies
  working-directory: ./API
  run: dotnet restore

- name: Restore Client Dependencies
  working-directory: ./Client
  run: dotnet restore

- name: Build API
  working-directory: ./API
  run: dotnet build --no-restore --configuration Release

- name: Build Client
  working-directory: ./Client
  run: dotnet build --no-restore --configuration Release

- name: Run Static Code Analysis (API)
  working-directory: ./API
  run: dotnet format --verify-no-changes --severity error

- name: Run Static Code Analysis (Client)
  working-directory: ./Client
  run: dotnet format --verify-no-changes --severity error
```

### Stage 6: Dependency Vulnerability Scans (.NET)

```yaml
- name: Dependency Vulnerability Scan (API)
  working-directory: ./API
  run: dotnet list package --vulnerable

- name: Dependency Vulnerability Scan (Client)
  working-directory: ./Client
  run: dotnet list package --vulnerable
```

### Stage 7: Docker Compose Up & Down

```yaml
- name: Run Docker Compose
  run: docker-compose -f docker-compose.yml up --build --abort-on-container-exit

- name: Shut Down Docker Compose
  if: always()
  run: docker-compose -f docker-compose.yml down -v
```

### Notes
- Make sure secrets are added in GitHub repository settings.
- Fix any vulnerabilities and secrets if found.
- Good formatting and dependency management are crucial for security and stability.
