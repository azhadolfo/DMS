FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN sed -i 's|http://archive.ubuntu.com/ubuntu|https://archive.ubuntu.com/ubuntu|g; s|http://security.ubuntu.com/ubuntu|https://security.ubuntu.com/ubuntu|g' /etc/apt/sources.list.d/ubuntu.sources \
    && apt-get update -o Acquire::Retries=5 \
    && apt-get install -y --no-install-recommends \
        -o Acquire::Retries=5 \
        ghostscript \
        python3 \
        python3-pip \
        qpdf \
        tesseract-ocr \
        tesseract-ocr-eng \
    && python3 -m pip install --no-cache-dir --break-system-packages ocrmypdf \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DocumentManagement.csproj", "./"]
RUN dotnet restore "DocumentManagement.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "./DocumentManagement.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./DocumentManagement.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
USER $APP_UID

ENTRYPOINT ["dotnet", "DocumentManagement.dll"]
