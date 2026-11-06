#!/usr/bin/env python3
"""Turn `dotnet publish` output into a GitHub Pages site served from /<repo>/.

    python3 tools/prepare-pages.py output/wwwroot /NxtShift/

- writes the real <base href> into index.html (so the browser's preload scanner asks for the right URLs),
- updates index.html's integrity hash in service-worker-assets.js (the service worker refuses a mismatch),
- copies index.html to 404.html (Pages serves it for deep links like /NxtShift/running),
- adds .nojekyll (otherwise Pages drops _framework and _content),
- removes the precompressed copies of the edited files (Pages doesn't serve .br/.gz; stale ones would only confuse).
"""
import base64, hashlib, pathlib, re, sys

root = pathlib.Path(sys.argv[1])
base = sys.argv[2]
if not (base.startswith("/") and base.endswith("/")):
    sys.exit("base must start and end with '/'")

index = root / "index.html"
html = index.read_text(encoding="utf-8")
html, n = re.subn(r'<base href="[^"]*"\s*/>', f'<base href="{base}" />', html, count=1)
if n != 1:
    sys.exit("no <base href> in index.html")
index.write_text(html, encoding="utf-8")

digest = "sha256-" + base64.b64encode(hashlib.sha256(index.read_bytes()).digest()).decode()
manifest = root / "service-worker-assets.js"
text = manifest.read_text(encoding="utf-8")
text, n = re.subn(r'("hash":\s*")[^"]+("\s*,\s*"url":\s*"index\.html")', lambda m: m.group(1) + digest + m.group(2), text)
if n != 1:
    sys.exit("index.html not found in service-worker-assets.js")
manifest.write_text(text, encoding="utf-8")

(root / "404.html").write_bytes(index.read_bytes())
(root / ".nojekyll").touch()
for name in ("index.html", "service-worker-assets.js"):
    for ext in (".br", ".gz"):
        (root / (name + ext)).unlink(missing_ok=True)

print(f"Prepared {root} for {base} ({digest})")
