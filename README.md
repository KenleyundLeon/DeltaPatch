# <div align="center">DeltaPatch - Automate Your Updates</div>
<div align="center">
<a href="https://github.com/northwood-studios/LabAPI"><img src="https://image2url.com/images/1759563390122-d4824ef5-f596-4c20-9063-2c606a16971c.png"></a>
<a href="https://github.com/KenleyundLeon/DeltaPatch"><img src="https://image2url.com/images/1759565889245-ff2e02c2-1f19-4f72-bc06-43a3b77fb4bd.png"></a>  

Tired of updating all your plugins manually? Let's automate it!

<img src="https://image2url.com/images/1759611430674-abc9ea56-8150-475c-a673-24db66c2b634.png" style="width:200px; height:200px;"> 
</div>

## Quick Tutorial
<details>
  <summary>Show instructions</summary>

### How to Install

1. Download the latest release file named **`DeltaPatch.dll`**.  
2. Copy the file to: `.config/SCP Secret Laboratory/LabAPI/plugins/{port}` or `.config/SCP Secret Laboratory/LabAPI/plugins/global`.
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

### What if you’re using an organization?
Change the **resource owner** from your personal account to the organization before generating the token.
</details>

## Compatible Plugins
<details>
  <summary>Show compatible plugins</summary>

### THESE PLUGINS ARE FULLY TESTED AND WORK WITH EVERY VERSION ABOVE 2.0

- [Wireless-Keycards](https://github.com/KenleyundLeon/Wireless-Keycards)
- [StatsSystem](https://github.com/MedveMarci/StatsSystem)
- [VPNGuard](https://github.com/MedveMarci/VPNGuard)
- [RespawnTimer](https://github.com/MedveMarci/RespawnTimer)
- [Scp999](https://github.com/MedveMarci/Scp999)
- [Scp066](https://github.com/MedveMarci/Scp066)
- [Push-SCPSL](https://github.com/tayjay/Push-SCPSL)
- [Talky](https://github.com/tayjay/Talky)
- [SCPDiscord](https://github.com/KarlOfDuty/SCPDiscord)
</details>

## Plugin Commands
<details>
  <summary>Show plugin commands</summary>

  ### DeltaPatch Commands

  - `deltapatch version` — Displays the current version of DeltaPatch.
  - `deltapatch infos` — Shows information about all loaded plugins.

</details>

## For Developers
<details>
  <summary>Show developer guide</summary>

### Adding Compatibility
- Adding DeltaPatch compatibility is easy.  
- Create a new public string value in your main file where the `Plugin` interface is used:  
  ```cs
  public string githubRepo = "CHANGE THIS TO YOUR GITHUB REPOSITORY"; // example: KenleyundLeon/DeltaPatch
  ```
- Example image:  
  <img src="https://image2url.com/images/1759612903745-72d179ea-0dc5-4a45-93fd-efa463b5f760.png">

### Adding Compatibility with AssemblyMetadata
- You can now add an AssemblyMetadata attribute in your AssemblyInfo.cs and recompile your plugin.
- Your metadata should follow this format: `[assembly: AssemblyMetadata("RepositoryUrl", "https://github.com/YOUR-GITHUB-HERE")]`
- Example image:
  <img src="https://image2url.com/images/1759861785442-8de033e8-3476-4d5d-9790-f2f03c041793.png">
- This also works for `[assembly: AssemblyCompany("https://github.com/YOUR-GITHUB-HERE")]`

### Adding Custom Dependencies
- It's easy: just put a `dependencies.zip` file in your release containing all required dependencies.
- Make sure all .dll files are **in the main zip folder**, not inside any subfolders.

### Compatibility Badge
```html
<a href="https://github.com/KenleyundLeon/DeltaPatch"><img src="https://image2url.com/images/1759565889245-ff2e02c2-1f19-4f72-bc06-43a3b77fb4bd.png"></a>
```
<a href="https://github.com/KenleyundLeon/DeltaPatch"><img src="https://image2url.com/images/1759565889245-ff2e02c2-1f19-4f72-bc06-43a3b77fb4bd.png"></a>

Or

```html
[![DeltaPatch Compatible](https://img.shields.io/badge/DeltaPatch-Compatible-brightgreen.svg)](https://github.com/KenleyundLeon/DeltaPatch/releases)
```
[![DeltaPatch Compatible](https://img.shields.io/badge/DeltaPatch-Compatible-brightgreen.svg)](https://github.com/KenleyundLeon/DeltaPatch/releases)

⚠️ **USE ONLY IF YOUR PLUGIN IS COMPATIBLE!** ⚠️
</details>

## FAQ
<details>
  <summary>Frequently Asked Questions</summary>

### Debug logs say I'm in a timeout — what should I do?
- You should **raise** the update timer in the config by 1–2 minutes.  
- You can also add a **GitHub Personal Access Token**, which helps avoid GitHub timeout issues.

### Some plugins aren't updating — what's the issue?
- Try installing the **latest release**.  
  `AssemblyMetadata` checks (which some plugins use) were only added in **version 2.0**.

### GitHub issues don’t work / I can’t add a tag — how can I contact you?
- You can reach me (**Kenley M.**) via Discord: `m_kenley`  
  or via email: [kenley@froback.de](mailto:kenley@froback.de)

### Can I pay you to make a plugin for my server?
- Of course — just contact me!  
  However, I **only code for LabAPI**, **not** for Exiled.

</details>

## Licensing
This project is licensed under the **Apache License 2.0**.  
You are allowed to fork this repository and make your own integrations, **but you must give proper credit** to the original author.

## Credits
- [Northwood Studios](https://github.com/northwood-studios)  
- [LabAPI Team](https://github.com/northwood-studios/LabAPI)