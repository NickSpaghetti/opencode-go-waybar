.DEFAULT_GOAL := help

# Override from the command line or environment, not from the working tree.
IMAGE ?= opencode-go-waybar-dev
RUNTIME ?= linux-x64
CONFIGURATION ?= Release
DOCKERFILE ?= Dockerfile
DOTNET ?= dotnet
DOCKER ?= docker

.PHONY: help
help: ## Show available targets
	@awk 'BEGIN {FS = ":.*##"; printf "Targets:\n"} /^[a-zA-Z_-]+:.*##/ {printf "  \033[36m%-15s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)

.PHONY: build-dev
build-dev: ## Build the dev container image
	$(DOCKER) build --target dev -f $(DOCKERFILE) -t $(IMAGE) .

.PHONY: build-prod
build-prod: ## Build the prod verification container image
	$(DOCKER) build --target final -f $(DOCKERFILE) -t $(IMAGE)-prod .

.PHONY: build
build: ## Build the NativeAOT binary and the prod verification image
build: build-prod
	@echo "Wrote $(IMAGE)-prod image. NativeAOT binary is inside it."

.PHONY: test
test: ## Run the unit test suite inside the dev container
test: build-dev
	$(DOCKER) run --rm -v $(CURDIR):/workspace -w /workspace $(IMAGE) $(DOTNET) test tests/OpencodeGoWaybar.UnitTests/OpencodeGoWaybar.UnitTests.csproj --nologo

.PHONY: shell
shell: ## Open an interactive shell in the dev container
shell: build-dev
	$(DOCKER) run --rm -it -v $(CURDIR):/workspace -w /workspace $(IMAGE) /bin/bash

.PHONY: dev
dev: ## Run the source application in the dev container
dev: build-dev
	$(DOCKER) run --rm -i -v $(CURDIR):/workspace -w /workspace $(IMAGE) $(DOTNET) run --project src/OpencodeGoWaybar/OpencodeGoWaybar.csproj

.PHONY: publish
publish: ## Publish the NativeAOT binary into an output directory
publish: build-dev
	$(DOCKER) run --rm -v $(CURDIR):/workspace -w /workspace $(IMAGE) $(DOTNET) publish src/OpencodeGoWaybar/OpencodeGoWaybar.csproj --configuration $(CONFIGURATION) --runtime $(RUNTIME) --output /workspace/out/$(RUNTIME)

.PHONY: hooks
hooks: ## Configure this repository to use .githooks
hooks:
	git config core.hooksPath .githooks

.PHONY: secret-scan
secret-scan: ## Scan staged content for credentials
secret-scan:
	@echo "secret-scan stub: implement in stage/contracts"

.PHONY: clean
clean: ## Remove generated build output
clean:
	-$(DOCKER) image rm -f $(IMAGE) $(IMAGE)-prod 2>/dev/null || true
	rm -rf out