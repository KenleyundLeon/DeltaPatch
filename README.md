# <div align="center">DeltaPatch - Automate Your Updates</div>
<div align="center">
<a href="https://github.com/northwood-studios/LabAPI"><img src="https://image2url.com/images/1759563390122-d4824ef5-f596-4c20-9063-2c606a16971c.png"></a>

Tired of updating all your plugins manually? Let's automate it!

<img src="https://image2url.com/images/1759611430674-abc9ea56-8150-475c-a673-24db66c2b634.png" style="width:200px; height:200px;"> 
</div>

## Quick Tutorial
<details>
  <summary>Show instructions</summary>

### How to Install

1. Download the latest release file named **`DeltaPatch.dll`**.  
2. Copy the file to: `.config/SCP Secret Laboratory/LabAPI/config/{port}`.

⚠️ This step is important — DeltaPatch is **not ready for global installation** yet.

3. Restart your server. After the reboot, a configuration file will be created at: `.config/SCP Secret Laboratory/LabAPI/config/{port}/DeltaPatch/config.yml`
4. You can customize how often DeltaPatch checks for updates and when the server should reboot to apply changes.  
5. That’s it!  
All plugins that include this badge:  
<a href="https://github.com/KenleyundLeon/DeltaPatch"><img src="https://image2url.com/images/1759565889245-ff2e02c2-1f19-4f72-bc06-43a3b77fb4bd.png" width="200" height="60"></a>  
will be **automatically updated** to their latest release version.

### How to Add Private Repositories

1. Go to [GitHub Personal Access Tokens](https://github.com/settings/personal-access-tokens).  
2. Generate a **new token**.  
3. Give the token a name and select the **repositories** you want to access.  
4. Under permissions, enable **Contents** and **Metadata**.  
5. Generate the token and **copy** it.  
6. Paste it into the `github_api_key` field located at: `.config/SCP Secret Laboratory/LabAPI/plugins/{port}/DeltaPatch/config.yml`
7. Restart your server — your plugins will now **automatically update** to the latest release.
</details>
  
## For Developers
<details>
  <summary>Show developer guide</summary>

### Adding Compatibility
- Adding DeltaPatch compatibility is easy.  
- Create a new public string value in your main file where the `Plugin` interface is used:  
  public string githubRepo = "CHANGE THIS TO YOUR GITHUB REPOSITORY";  
- Example image:  
  <img src="https://image2url.com/images/1759612903745-72d179ea-0dc5-4a45-93fd-efa463b5f760.png">

### Compatibility Badge
```html
<a href="https://github.com/KenleyundLeon/DeltaPatch"><img src="https://image2url.com/images/1759565889245-ff2e02c2-1f19-4f72-bc06-43a3b77fb4bd.png"></a>
```
⚠️ **USE ONLY IF YOUR PLUGIN IS COMPATIBLE!** ⚠️
</details>

## Licensing
This project is licensed under the **Apache License 2.0**.  
You are allowed to fork this repository and make your own integrations, **but you must give proper credit** to the original author.

## Credits
- [Northwood Studios](https://github.com/northwood-studios)  
- [LabAPI Team](https://github.com/northwood-studios/LabAPI)
