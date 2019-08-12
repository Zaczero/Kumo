# ![Kumo small logo](https://github.com/Zaczero/Kumo/blob/master/images/KumoSmall.png) Kumo - DDoS mitigation

Kumo is a project started in order to provide a free, open-sourced and reliable solution in DDoS mitigation.

While creating Kumo I was thinking about it as a better alternative to *fail2ban* software.
*fail2ban* is great in blocking abusing users at a small scale but when there are thousands of requests per seconds it starts to struggle quite a lot with CPU spiking to 90%-98% and basically killing the server.
Kumo in the same scenario can keep the CPU usage around 1%-5% and it has some nice bonus features like enabling Under Attack Mode in Cloudflare.

### 🔴 Requirements

* .NET Core 2.2
* Cloudflare
* Linux server
* Nginx

### 👨‍💻 How it works

A basic infographic to better visualise what's going on under the hood.  
Please keep in mind that this is a very simplified example.

![Kumo infographic](https://github.com/Zaczero/Kumo/blob/master/images/KumoInfo.png)

### 🎡 Features

* Lightweight & fast
* Supports both IPv4 and IPv6
* Mitigates both Layer7 *(HTTP)* DoS and DDoS attacks
* Enables Cloudflare Under Attack Mode when massive DDoS is detected *(optional)*

### 🏁 Installation

1. Install [.NET Core 2.2](https://dotnet.microsoft.com/download/linux-package-manager/ubuntu18-04/runtime-2.2.0) on your machine
2. [Download latest Kumo release](https://github.com/Zaczero/Kumo/releases/latest) and unzip it
3. Make sure you are fine with where Kumo files are located *(you won't be able to move it without full reinstallation)*
4. Open Kumo installation directory *(so you can see Kumo.dll etc. files after executing `ls` command)*
5. Make `install-service.sh` file executable using `sudo chmod +x install-service.sh` command
6. And then execute it with `sudo ./install-service.sh`
7. Configure Kumo by editing `config.json` file *([documentation](#documentation))*
8. Configure Nginx to work with Kumo *([tutorial](#configuring-nginx))*
9. Test your configuration with `dotnet Kumo.dll` command *(Ctrl+C to exit)*
10. Start service by running `sudo systemctl start kumo`

Looking for uninstallation instructions? [Click here](#uninstallation).

### 🔧 Configuring Nginx

1. First of all make sure that the `nginx -s reload` command is working properly
2. Edit the Nginx configuration `nginx.conf` file and add the following line `include /etc/nginx/snippets/kumo.conf;`
   *(path must be the same as *NginxBlockSnippetFile* from Kumo `config.json` file)*
3. Double-check that you have configured [Nginx rate limiting](https://www.nginx.com/blog/rate-limiting-nginx/) properly
   and that you are getting [user's real ip](https://www.mysterydata.com/how-to-get-the-real-ip-address-using-cloudflare-and-nginx-cwp-centos-web-panel/) from the Cloudflare header
   *([latest Cloudflare IP ranges](https://www.cloudflare.com/ips/))*

### 📬 Contact

* Email: kamil@monicz.pl

### ☕ Support me

* Bitcoin: `35n1y9iHePKsVTobs4FJEkbfnBg2NtVbJW`
* Ethereum: `0xc69C7FC9Ce691c95f38798506EfdBB8d14005B67`

### 🛠️ Documentation

* **CloudflareEmail**  
Your cloudlfare account email address

* **CloudflareApiKey**  
Your cloudlfare account global API key  
Tutorial - [how to find it](#how-to-find-your-cloudflare-api-key)

* **CloudflareUnderAttackMode**  
Enable Cloudflare's Under Attack Mode when massive attack is detected

* **CloudflareModeDefault**  
Default security level *(switch to it after Under Attack Mode expires)* *(to use only with CloudflareUnderAttackMode)*

* **CloudflareManageZones**  
List of zones/websites where Under Attack Mode should be enabled *(to use only with CloudflareUnderAttackMode)*  
Tutorial - [how to find it](#how-to-find-your-cloudflare-zone-id)

Example configuration:
```json-with-comments
"CloudflareManageZones": [
  "12345678901234567890123456789012",
  "67175678901234567890123456784824",
  "85295678901234567890123456783270" // <-- last one doesn't have a ','
],
```

* **BlockNote**  
Comment to set in Cloudflare block rule and Nginx block .conf file

* **WatcherTargetFile**  
Full path to Nginx `error.log` file

* **WatcherCheckSleep**  
Check for `error.log` file changes every **X** milliseconds *(2 seconds = 2000)*

* **AbuseExpirationTime**  
Time after which abuse counter resets to zero *(value in seconds, 5 minutes = 300)*

* **BlockExpirationTime**  
Time after which IP is removed from the blacklist *(value in seconds, 3 hours = 10800)*

* **BlocksToUnderAttack**  
Amount of blocks required in single tick to enable Under Attack Mode *(1 tick = WatcherCheckSleep milliseconds)* *(to use only with CloudflareUnderAttackMode)*

* **UnderAttackExpirationTicks**  
Ticks after which Under Attack Mode is disabled *(1 tick = WatcherCheckSleep milliseconds)* *(to use only with CloudflareUnderAttackMode)*

* **AbusesToBlock**  
How many abuses are required to add IP to the blacklist

* **AbusesToBlockUnderAttack**  
How many abuses are required to add IP to the blacklist while Under Attack Mode is enabled *(to use only with CloudflareUnderAttackMode)*

* **NginxBlockSnippetFile**  
Full path where Nginx block .conf file will be created

### 🤔 How to find your Cloudflare API key

1. [Login](https://dash.cloudflare.com/) to your Cloudflare account
2. Go to [My Profile](https://www.cloudflare.com/a/account/my-account)
3. Switch tab to `API Tokens` and scroll down to `API Keys` section
4. Click `View` button next to `Global API Key`

![Cloudflare API key](https://github.com/Zaczero/Kumo/blob/master/images/CloudflareApiKey.png)

### ❓ How to find your Cloudflare zone ID

1. [Login](https://dash.cloudflare.com/) to your Cloudflare account
2. Go to overview of your website
3. Scroll down to `API` section

![Cloudflare zone ID](https://github.com/Zaczero/Kumo/blob/master/images/CloudflareZoneId.png)

### 👋 Uninstallation

1. Stop and disable Kumo service by executing `sudo systemctl stop kumo && sudo systemctl disable kumo`
2. Remove service file with `sudo rm /lib/systemd/system/kumo.service`
3. Now you can safely delete all Kumo files

### 📃 License

* [Zaczero/Kumo](https://github.com/Zaczero/Kumo/blob/master/LICENSE)
* [JamesNK/Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)

*Robot vector created by [rawpixel.com](https://rawpixel.com)*
