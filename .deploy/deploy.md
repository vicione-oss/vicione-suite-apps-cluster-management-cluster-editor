# Deployment

## SSH Keys

The ssh key `deploy_ed25519` in this repository are meant to be used for accessing deployment devices. The public key `deploy_ed25519.pub` needs to be available to the target systems ssh configuration.

## SSH User

The ssh user requires sudo access for the deployment. The deplyoment target system needs to configure `sudo` to not require login / password.

Example sudo config:

`/etc/sudoers.d/deploy`
```bash
deploy ALL=(ALL) NOPASSWD: ALL
```

## Systemd Service Unit

The cluster editor systemd service unit is configured in `cluster-editor.service`. It expects the software to be installed in `/opt/cluster-editor/Server`.
