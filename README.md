# Roblox Asset Downloader
RbxAsset CLI is an open-source command-line tool for downloading and working with Roblox assets. Built with developers and creators in mind, RbxAsset CLI provides a simple and efficient way to fetch Roblox assets directly from the command line, making it useful for asset pipelines, automation, tooling, and development workflows.

## Example Usage
- Help:
```sh
rbxasset-cli help
```
- Getting Character by Username:
```sh
rbxasset-cli character --key <KEY>::<api/auth> --username <username>
```
- Getting Character by UserId:
```sh
rbxasset-cli character --key <KEY>::<api/auth> --user-id <user_id>
```
- Getting Item:
```sh
rbxasset-cli item --key <KEY>::<api/auth> --item-id <item_id>
```
- Getting Bundle:
```sh
rbxasset-cli bundle --key <KEY>::<api/auth> --bundle-id <bundle_id>
```
- Getting Rbxm File:
```sh
rbxasset-cli model --key <KEY>::<api/auth> --model-id <model_id>
```
- Parse RbxAssetUrl to AssetType and the AssetId:
```sh
rbxasset-cli rbx-asset --key <KEY>::<api/auth> --asset-url rbxassetid://<asset_id>
```
- Getting Image Asset:
```sh
rbxasset-cli image --key <KEY>::<api/auth> --image-id <asset_id>
```
- Getting Mesh Asset:
```sh
rbxasset-cli mesh --key <KEY>::<api/auth> --mesh-id <mesh_id>
```

## Contributors
<a href="https://github.com/Faizdzn/rbxasset-cli/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Faizdzn/rbxasset-cli" />
</a>

Made with [contrib.rocks](https://contrib.rocks).

## Donate Us
If RbxAssetCli is useful to you, consider supporting its development! Your support helps us maintain the project, improve existing features, fix bugs, and continue building useful open-source tools for the community. Every contribution, whether it's a sponsorship, donation, or simply sharing the project, is greatly appreciated.

[![Patreon](https://img.shields.io/badge/dynamic/json?url=https%3A%2F%2Fwww.patreon.com%2Fapi%2Fcampaigns%2F15749420&query=data.attributes.patron_count&suffix=%20Patrons&color=FF5441&label=Patreon&logo=Patreon&logoColor=FF5441&style=for-the-badge)](https://patreon.com/Faizdzn)