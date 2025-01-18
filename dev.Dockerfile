FROM debian:bookworm

WORKDIR /app
RUN apt update && \
    apt install -y wget libicu-dev git && \
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh && \
    chmod u+x dotnet-install.sh && \
    ./dotnet-install.sh && \
    rm ./dotnet-install.sh
ENV PATH=/root/.dotnet:$PATH

EXPOSE 5242
