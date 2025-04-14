FROM mcr.microsoft.com/dotnet/sdk:8.0 AS server-build
WORKDIR /app
COPY ./src . 
RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM debian:bookworm AS frontend-build
ENV NODE_VERSION=22.13.0
RUN apt-get update && \
    apt-get install -y curl && \
    curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash
ENV NVM_DIR=/root/.nvm
RUN . "$NVM_DIR/nvm.sh" && nvm install ${NODE_VERSION}
ENV PATH="/root/.nvm/versions/node/v${NODE_VERSION}/bin/:${PATH}"
RUN npm install -g @angular/cli
WORKDIR /app
COPY ./frontend .
RUN ng build --configuration production

FROM debian:bookworm

# install supervisord
RUN apt-get update && \
    apt-get install -y supervisor && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*
RUN mkdir -p /etc/supervisor/conf.d /var/log/supervisor
COPY ./files/supervisord.conf /etc/supervisord.conf

# install transmission
RUN apt-get update && \
    apt-get install -y transmission-cli transmission-daemon && \
    rm -rf /var/lib/apt/lists/*
COPY ./files/transmission-daemon/settings.json /root/.config/transmission-daemon/settings.json

# install imdl
RUN apt-get update && \
    apt-get install -y wget curl && \
    wget https://imdl.io/install.sh -O imdl-install.sh && \
    chmod u+x imdl-install.sh && \
    ./imdl-install.sh && \
    rm imdl-install.sh && \
    rm -rf /var/lib/apt/lists/*
ENV PATH=/root/bin:$PATH

# install dotnet
RUN apt-get update && \
    apt-get install -y libicu-dev && \
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod u+x dotnet-install.sh && \
    ./dotnet-install.sh && \
    rm ./dotnet-install.sh && \
    rm -rf /var/lib/apt/lists/*
ENV PATH=/root/.dotnet:$PATH

# install jackett
RUN wget https://github.com/Jackett/Jackett/releases/download/v0.22.1775/Jackett.Binaries.LinuxARM64.tar.gz && \
    tar -xvzf Jackett.Binaries.LinuxARM64.tar.gz && \
    rm Jackett.Binaries.LinuxARM64.tar.gz
COPY ./files/Jackett/ServerConfig.json /root/.config/Jackett/ServerConfig.json

WORKDIR /app
COPY --from=server-build /app/out .
COPY --from=frontend-build /app/dist/frontend/browser ./wwwroot
EXPOSE 80 9091 9117 51413 51413/udp
CMD ["/usr/bin/supervisord", "-c", "/etc/supervisord.conf"]
