# simplequizapi


# 環境情報

| 機能 | バージョン |
| ---- | ---- |
| Linux/Ubuntu | 20.4.* |
| .NET | 6.0 |
| C# | .NET依存 |
| ASP.NET | 6.2.3 |


# 環境構築


## .NET環境のインストール

```bash
wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install -y dotnet-sdk-6.0
```

プロジェクトの実行

```bash
dotnet run
```


## SQL Serverのインストール

Ubuntu 22.4.*系だと正常にインストールできません。

```bash
sudo apt-get install wget

wget -qO- https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -

sudo apt-get install software-properties-common

sudo add-apt-repository "$(wget -qO- https://packages.microsoft.com/config/ubuntu/20.04/mssql-server-preview.list)"

sudo apt-get update
sudo apt-get install -y mssql-server
```



```bash
sudo /opt/mssql/bin/mssql-conf setup

# -> 3 (Express)
# -> simplequizapi_pw1234

systemctl status mssql-server --no-pager
```

---

sqlcmdを入れる。


```bash
curl https://packages.microsoft.com/keys/microsoft.asc | sudo apt-key add -
curl https://packages.microsoft.com/config/ubuntu/20.04/prod.list | sudo tee /etc/apt/sources.list.d/msprod.list

sudo apt-get update
sudo apt-get install mssql-tools unixodbc-dev

sudo apt-get update 
sudo apt-get install mssql-tools

echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bash_profile
echo 'export PATH="$PATH:/opt/mssql-tools/bin"' >> ~/.bashrc
source ~/.bashrc

sqlcmd -S localhost -U sa -P 'simplequizapi_pw1234'
```




# SSL/TLS証明書

無料で使用できる「Let's Encrypt」を使用します。

```bash
sudo apt install certbot
sudo certbot certonly --standalone -d api.simple-quiz.org
```

更新は以下の手順で♪

```bash
# 80番ポートを解放
lsof -i -P
kill -9 プロセス

sudo certbot renew
```



# C#ライブラリ群のインストール

```bash
dotnet add package System.Data.SqlClient


```





# API仕様書

- [Postman](https://simple-quiz-api.postman.co/workspace/)



# 参考文献

- [Ubuntuに.NETをインストールする方法](https://learn.microsoft.com/ja-jp/dotnet/core/install/linux-ubuntu)
- [.NETアプリケーションの実行](https://learn.microsoft.com/ja-jp/troubleshoot/developer/webapps/aspnetcore/practice-troubleshoot-linux/2-1-create-configure-aspnet-core-applications)
- [MinimalAPIの概要](https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-6.0)
- [SQL Serverのインストール](https://learn.microsoft.com/ja-jp/sql/linux/quickstart-install-connect-ubuntu?view=sql-server-ver16)
