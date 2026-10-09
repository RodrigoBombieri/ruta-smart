"""Check local documentation links and the documented HTTP surface."""
import json
import re
from pathlib import Path
from urllib.parse import unquote, urlsplit

root = Path(__file__).resolve().parent.parent
documents = [*root.glob('*.md'), *root.glob('docs/**/*.md'), root / 'mobile/rutasmart/README.md']
errors = []
for document in documents:
    content = document.read_text(encoding='utf-8')
    targets = re.findall(r'\]\(([^\s)]+)', content) + re.findall(r'(?:src|href)="([^"]+)"', content)
    for target in targets:
        url = urlsplit(target.strip('<>'))
        if url.scheme or not url.path:
            continue
        path = (document.parent / unquote(url.path)).resolve()
        if not path.exists():
            errors.append(f'{document.relative_to(root)}: missing {target}')

spec = json.loads((root / 'docs/openapi.json').read_text(encoding='utf-8'))


def inspect(value):
    if isinstance(value, dict):
        for key, child in value.items():
            if key == '$ref' and child.startswith('#/'):
                current = spec
                try:
                    for part in child[2:].split('/'):
                        current = current[part]
                except KeyError:
                    errors.append(f'Unresolved schema: {child}')
            else:
                inspect(child)
    elif isinstance(value, list):
        for child in value:
            inspect(child)


inspect(spec)
program = (root / 'src/RutaSmart.Api/Program.cs').read_text(encoding='utf-8')
actual = {(method.lower(), path.replace(':guid', '')) for method, path in re.findall(r'app.Map(Get|Post|Delete)\("([^"]+)"', program)}
documented = {(method, path) for path, methods in spec['paths'].items() for method in methods}
if actual != documented:
    errors.append(f'HTTP surface mismatch: {actual.symmetric_difference(documented)}')
for name in ('planner-desktop', 'report-desktop', 'planner-phone', 'report-phone'):
    path = root / f'docs/screenshots/{name}.png'
    if not path.exists() or path.read_bytes()[:8] != b'\x89PNG\r\n\x1a\n':
        errors.append(f'Missing real PNG screenshot: {name}')
if errors:
    raise SystemExit('\n'.join(errors))
print(f'{len(documents)} documents: local links, screenshots, schema references and HTTP surface OK.')
