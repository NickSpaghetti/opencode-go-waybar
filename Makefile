.DEFAULT_GOAL := help

# Override from the command line or environment, not from the working tree.
IMAGE ?= opencode-go-waybar-dev
RUNTIME ?= linux-x64
CONFIGURATION ?= Release
DOCKERFILE ?= Dockerfile
DOTNET ?= dotnet
DOCKER ?= docker
SPECMATIC_IMAGE ?= specmatic/specmatic:latest@sha256:99d73771b5bd2caddf43ab66ae463dbe207b22713eefa5943115d887cb5939d4
SPECMATIC_PORT ?= 9000
TEST_NETWORK ?= opencode-go-waybar-contract
SPECMATIC_CONTRACT ?= contracts/opencode-go-usage.openapi.yaml
NUGET_CACHE ?= $(HOME)/.cache/opencode-go-waybar/nuget

# Run every container as the host user so bind-mounted build artifacts stay
# owned by the user who runs Make. The dev image still installs SDK packages
# as root during `docker build`; only the runtime invocations drop privileges.
DOCKER_USER := $(shell id -u):$(shell id -g)

DOCKER_RUN = $(DOCKER) run --rm --user $(DOCKER_USER) \
	-e NUGET_PACKAGES=/tmp/opencode-go-waybar/nuget \
	-v $(CURDIR):/workspace \
	-v $(NUGET_CACHE):/tmp/opencode-go-waybar/nuget \
	-w /workspace

.PHONY: prepare-cache
prepare-cache:
	mkdir -p "$(NUGET_CACHE)"

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
test: build-dev prepare-cache
	$(DOCKER_RUN) $(IMAGE) $(DOTNET) test tests/OpencodeGoWaybar.UnitTests/OpencodeGoWaybar.UnitTests.csproj --nologo

.PHONY: shell
shell: ## Open an interactive shell in the dev container
shell: build-dev prepare-cache
	$(DOCKER) run --rm --user $(DOCKER_USER) -e NUGET_PACKAGES=/tmp/opencode-go-waybar/nuget -it \
		-v $(CURDIR):/workspace -v $(NUGET_CACHE):/tmp/opencode-go-waybar/nuget \
		-w /workspace $(IMAGE) /bin/bash

.PHONY: dev
dev: ## Run the source application in the dev container
dev: build-dev prepare-cache
	$(DOCKER_RUN) -i $(IMAGE) $(DOTNET) run --project src/OpencodeGoWaybar/OpencodeGoWaybar.csproj

.PHONY: contract
contract: ## Start Specmatic container on the isolated contract network
contract:
	-$(DOCKER) network rm $(TEST_NETWORK) 2>/dev/null || true
	$(DOCKER) network create $(TEST_NETWORK)
	$(DOCKER) run -d --rm --name $(IMAGE)-specmatic --network $(TEST_NETWORK) -p $(SPECMATIC_PORT):9000 \
		-v $(CURDIR)/contracts:/app/contracts:ro \
		-w /app/contracts \
		$(SPECMATIC_IMAGE) \
		stub \
		--host 0.0.0.0 \
		--port 9000 \
		/app/contracts/opencode-go-usage.openapi.yaml
	@echo "Specmatic mock started on http://127.0.0.1:$(SPECMATIC_PORT)"

.PHONY: contract-stop
contract-stop: ## Stop the Specmatic container
contract-stop:
	-$(DOCKER) rm -f $(IMAGE)-specmatic 2>/dev/null || true
	-$(DOCKER) network rm $(TEST_NETWORK) 2>/dev/null || true

.PHONY: publish
publish: ## Publish the NativeAOT binary into an output directory
publish: build-dev prepare-cache
	$(DOCKER_RUN) $(IMAGE) $(DOTNET) publish src/OpencodeGoWaybar/OpencodeGoWaybar.csproj --configuration $(CONFIGURATION) --runtime $(RUNTIME) --output /workspace/out/$(RUNTIME)

.PHONY: hooks
hooks: ## Configure this repository to use .githooks
hooks:
	git config core.hooksPath .githooks

.PHONY: secret-scan
secret-scan: ## Scan staged content for credentials
secret-scan:
	@echo "secret-scan stub: implement in stage/contracts"

.PHONY: clean
clean: ## Remove generated build output owned by the current user
clean:
	-$(DOCKER) image rm -f $(IMAGE) $(IMAGE)-prod 2>/dev/null || true
	-$(DOCKER) rm -f $(IMAGE)-specmatic 2>/dev/null || true
	-$(DOCKER) network rm -f $(TEST_NETWORK) 2>/dev/null || true
	rm -rf out src/OpencodeGoWaybar/bin src/OpencodeGoWaybar/obj \
	       tests/OpencodeGoWaybar.UnitTests/bin tests/OpencodeGoWaybar.UnitTests/obj

.PHONY: clean-all
clean-all: ## Remove build output even when owned by root (uses sudo)
clean-all: clean
	-sudo rm -rf src/OpencodeGoWaybar/bin src/OpencodeGoWaybar/obj \
	           tests/OpencodeGoWaybar.UnitTests/bin tests/OpencodeGoWaybar.UnitTests/obj \
	           out
