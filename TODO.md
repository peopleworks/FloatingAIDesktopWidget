# TODO

## Packaging / Installer

- [ ] Create an installer (MSIX or WiX) for easier distribution.
- [ ] Add versioning + release artifacts (GitHub Releases).

## Startup

- [ ] Add “Run at startup” option (tray/menu).
  - Options to consider:
    - Startup folder shortcut (`shell:startup`)
    - Registry `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
    - Task Scheduler (more control, can request elevation if needed)

