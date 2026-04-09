# 1. Fail if any command fails
set -e

# Ensure directories exist
mkdir -p tds

# Ensure git is initialized
if [ ! -d ".git" ]; then
  git init && git add . && git commit -m "Initial commit"
fi

unset AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_SESSION_TOKEN

export AWS_PROFILE=atx # AWS profile to be used
export AWS_REGION=us-east-1 # Region for ATX
export ATX_SHELL_TIMEOUT=43200 # 12 hours in seconds

atx custom def exec -n "AWS/early-access-comprehensive-codebase-analysis" -p . -x -t
