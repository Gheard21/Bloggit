FROM mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm

# Install additional packages
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
        postgresql-client \
        git \
        curl \
        wget \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Install Entity Framework CLI tools globally
RUN dotnet tool install --global dotnet-ef

# Add dotnet tools to PATH
ENV PATH="$PATH:/home/vscode/.dotnet/tools"

# Install Docker Compose (for Testcontainers support)
RUN curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose \
    && chmod +x /usr/local/bin/docker-compose

# Set the working directory
WORKDIR /workspace