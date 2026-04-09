import requests
import json
import sys

def check_compatibility(package_id):
    """
    Queries NuGet V3 API to check if the latest version of a package
    is compatible with .NET 8 / .NET Standard.
    """
    # NuGet IDs are case-insensitive in search but lowercase in the flat container/registration API
    pkg_id_lower = package_id.lower()
    registration_url = f"https://api.nuget.org/v3/registration5-gz-semver2/{pkg_id_lower}/index.json"
    
    try:
        response = requests.get(registration_url, timeout=10)
        if response.status_code != 200:
            return "Unknown", "NOT_FOUND", "Manual Review"

        data = response.json()
        
        # The registration index groups versions into 'pages'. We want the last page for the latest versions.
        last_page = data['items'][-1]
        
        # If the page items are inlined, get the last one. If not, we'd need to fetch the page (simplified here).
        if 'items' in last_page:
            latest_entry = last_page['items'][-1]['catalogEntry']
        else:
            # Fetch external page if items are not inlined
            page_data = requests.get(last_page['@id']).json()
            latest_entry = page_data['items'][-1]['catalogEntry']

        version = latest_entry['version']
        dep_groups = latest_entry.get('dependencyGroups', [])
        
        # TFMs that indicate .NET 8 compatibility
        modern_tfms = [".net8.0", ".netstandard2.0", ".netstandard2.1", "netcoreapp3.1", "net6.0", "net7.0"]
        
        is_compatible = False
        supported_frameworks = []

        for group in dep_groups:
            tfm = group.get('targetFramework', '').lower()
            supported_frameworks.append(tfm)
            if any(m_tfm in tfm for m_tfm in modern_tfms):
                is_compatible = True
        
        # If no dependency groups exist, it might be a meta-package or old style; 
        # but usually, modern packages have them.
        status = "✅ YES" if is_compatible else "❌ NO"
        decision = "Update to " + version if is_compatible else "Replace/Rewrite"
        
        return version, status, decision

    except Exception as e:
        return "Error", "QUERY_FAILED", str(e)

def generate_report(packages):
    print("| Package ID | Latest Version | Compatible with net8? | Decision |")
    print("| :--- | :--- | :--- | :--- |")
    for pkg in packages:
        version, compat, decision = check_compatibility(pkg)
        print(f"| {pkg} | {version} | {compat} | {decision} |")

if __name__ == "__main__":
    # Example usage: python check_nuget_compat.py Autofac EntityFramework Newtonsoft.Json
    test_packages = sys.argv[1:] if len(sys.argv) > 1 else ["Autofac", "EntityFramework", "Newtonsoft.Json"]
    generate_report(test_packages)