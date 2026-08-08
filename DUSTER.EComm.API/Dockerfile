FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
#RUN apk add --no-cache tzdata icu-libs
COPY . .
ENTRYPOINT ["dotnet", "DUSTER.EComm.API.dll"]