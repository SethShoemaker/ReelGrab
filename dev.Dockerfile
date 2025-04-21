FROM debian:bookworm

WORKDIR /app
RUN apt update && apt install -y wget libicu-dev git curl

RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod u+x dotnet-install.sh && \
    ./dotnet-install.sh && \
    rm ./dotnet-install.sh
ENV PATH=/root/.dotnet:$PATH
EXPOSE 5242

ENV NODE_VERSION=22.13.0
RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.1/install.sh | bash
ENV NVM_DIR=/root/.nvm
RUN . "$NVM_DIR/nvm.sh" && nvm install ${NODE_VERSION}
ENV PATH="/root/.nvm/versions/node/v${NODE_VERSION}/bin/:${PATH}"
RUN npm install -g @angular/cli
EXPOSE 4200

RUN apt-get install -y transmission-cli transmission-daemon
COPY ./files/transmission-daemon/settings.json /root/.config/transmission-daemon/settings.json
EXPOSE 9091 51413 51413/udp

RUN wget https://github.com/casey/intermodal/releases/download/v0.1.14/imdl-v0.1.14-aarch64-unknown-linux-musl.tar.gz -O imdl.tar.gz && \
    tar -xvf imdl.tar.gz imdl && \
    mv ./imdl /bin/imdl && \
    rm imdl.tar.gz

WORKDIR /Jackett
RUN wget https://github.com/Jackett/Jackett/releases/download/v0.22.1775/Jackett.Binaries.LinuxARM64.tar.gz && \
    tar -xvzf Jackett.Binaries.LinuxARM64.tar.gz && \
    rm Jackett.Binaries.LinuxARM64.tar.gz
COPY ./files/Jackett/ServerConfig.json /root/.config/Jackett/ServerConfig.json
EXPOSE 9117