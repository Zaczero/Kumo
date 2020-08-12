# ![Kumo small logo](https://github.com/Zaczero/Kumo/blob/master/images/KumoSmall.png) Kumo - DDoS mitigation

[![Build Status](https://travis-ci.com/Zaczero/Kumo.svg?branch=master)](https://travis-ci.com/Zaczero/Kumo)

Kumo is a project started in order to provide a free, open-sourced and reliable solution in DDoS mitigation.

While creating Kumo I was thinking about it as a better alternative to *fail2ban* software.
*fail2ban* is great in blocking abusing users at a small scale but when there are thousands of requests per seconds it starts to struggle quite a lot with CPU spiking to 90%-98% and basically killing the server.
Kumo in the same scenario can keep the CPU usage around 1%-5% and it has some nice bonus features like enabling Under Attack Mode in Cloudflare.

### 🚗 Requirements

* .NET Core 2.1
* Cloudflare
* Linux server
* Nginx

### 🎡 Features

* Lightweight & fast
* Supports both IPv4 and IPv6
* Mitigates both Layer7 *(HTTP)* DoS and DDoS attacks
* Enables Cloudflare Under Attack Mode when massive DDoS is detected *(optional)*

### 👨‍💻 How does it work

A basic infographic to better visualise what's going on under the hood.  
Please keep in mind that this is a very simplified example.

![Kumo infographic](https://github.com/Zaczero/Kumo/blob/master/images/KumoInfo.png)

### 🏁 Installation

Check out this amazing [wiki article](https://github.com/Zaczero/Kumo/wiki/Installation-instructions)!  
It explains pretty much everything you need to know.

### 📬 Contact

* Email: kamil@monicz.pl

### 📃 License

* [Zaczero/Kumo](https://github.com/Zaczero/Kumo/blob/master/LICENSE)
* [JamesNK/Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json/blob/master/LICENSE.md)

*Robot vector created by [rawpixel.com](https://rawpixel.com)*
