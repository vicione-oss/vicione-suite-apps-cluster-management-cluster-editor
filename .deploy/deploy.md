# Deployment

## SSH Keys

The ssh key `deploy_ssh_prv` in this repositories CI secrets is meant to be used for accessing deployment devices. The public key `deploy_ssh_pub` needs to be available to the target systems ssh configuration.

## SSH User

The ssh user requires sudo access for the deployment. The deplyoment target system needs to configure `sudo` to not require login / password.

Example sudo config:

`/etc/sudoers.d/deploy`
```bash
deploy ALL=(ALL) NOPASSWD: ALL
```

## Systemd Service Unit

The cluster editor systemd service unit is configured in `cluster-editor.service`. It expects the software to be installed in `/opt/cluster-editor/Server`.
