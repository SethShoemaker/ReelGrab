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

RUN apt-get install -y transmission-cli
