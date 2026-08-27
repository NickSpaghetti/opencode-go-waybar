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

# Containerised test image. E2E_LAYER selects how much of the ACP client stack
# is built:
#   1  opencode (both installs) + the synthetic ACP client
#   2  + Neovim and CodeCompanion.nvim, a real third-party ACP client
#   3  + the VS Code CLI, used as a process-tree probe
#
# The image is linux/amd64 and carries the real NativeAOT linux-x64 binary; the
# acceptance tier runs that artifact, never a locally built one.
E2E_LAYER ?= 3
E2E_DOCKERFILE ?= Dockerfile.e2e
E2E_IMAGE ?= $(IMAGE)-e2e
INTEGRATION_PROJECT ?= tests/OpencodeGoWaybar.IntegrationTests/OpencodeGoWaybar.IntegrationTests.csproj
UI_PROJECT ?= src/OpencodeGoWaybar.Ui/OpencodeGoWaybar.Ui.csproj
UI_TEST_PROJECT ?= tests/OpencodeGoWaybar.Ui.UnitTests/OpencodeGoWaybar.Ui.UnitTests.csproj
ACCEPTANCE_PROJECT ?= tests/OpencodeGoWaybar.AcceptanceTests/OpencodeGoWaybar.AcceptanceTests.csproj
E2E_TARGET_1 := e2e-l1-synthetic
E2E_TARGET_2 := e2e-l2-neovim
E2E_TARGET_3 := e2e-l3-vscode
E2E_TARGET := $(E2E_TARGET_$(E2E_LAYER))
# The acceptance tier is layered; layer N runs its own tests and every layer below.
ACCEPTANCE_FILTER_1 := Layer=1
ACCEPTANCE_FILTER_2 := Layer=1|Layer=2
ACCEPTANCE_FILTER_3 := Layer=1|Layer=2|Layer=3
ACCEPTANCE_FILTER ?= $(ACCEPTANCE_FILTER_$(E2E_LAYER))
# Facts about opencode rather than about this module, so they are opt-in.
DEPENDENCY_FILTER ?= Tier=Dependency
# Needs a live OPENCODE_GO_API_KEY in the environment, so it is opt-in too.
USAGE_FILTER ?= Requires=ApiKey
INTEGRATION_FILTER ?= Tier=Integration

# Run every container as the host user so bind-mounted build artifacts stay
# owned by the user who runs Make. The dev image still installs SDK packages
# as root during `docker build`; only the runtime invocations drop privileges.
DOCKER_USER := $(shell id -u):$(shell id -g)

DOCKER_RUN = $(DOCKER) run --rm --user $(DOCKER_USER) \
	-e NUGET_PACKAGES=/tmp/opencode-go-waybar/nuget \
	-e DOTNET_CLI_HOME=/tmp/dotnet-cli-home \
	-v $(CURDIR):/workspace \
	-v $(NUGET_CACHE):/tmp/opencode-go-waybar/nuget \
	-w /workspace

# The e2e image needs a writable HOME of its own: opencode writes into it, and
# so does the .NET CLI once the container drops to the host user.
DOCKER_RUN_E2E = $(DOCKER) run --rm --platform=linux/amd64 --user $(DOCKER_USER) \
	-e NUGET_PACKAGES=/tmp/opencode-go-waybar/nuget \
	-e OPENCODE_GO_API_KEY \
	-e HOME=/tmp/e2e-home \
	-e DOTNET_CLI_HOME=/tmp/e2e-home \
	-e E2E_WORKSPACE=/workspace \
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

.PHONY: build-e2e
build-e2e: ## Build the containerised test image (E2E_LAYER=1|2|3)
	$(DOCKER) build --platform=linux/amd64 --target $(E2E_TARGET) -f $(E2E_DOCKERFILE) \
		-t $(E2E_IMAGE):l$(E2E_LAYER) .

.PHONY: integration
integration: ## Run the integration tier (real broker + service vs the real OS)
integration: build-e2e prepare-cache
	$(DOCKER_RUN_E2E) $(E2E_IMAGE):l$(E2E_LAYER) \
		$(DOTNET) test $(INTEGRATION_PROJECT) --nologo --filter "$(INTEGRATION_FILTER)"

.PHONY: acceptance
acceptance: ## Run the acceptance tier against the NativeAOT binary in the image
acceptance: build-e2e prepare-cache
	$(DOCKER_RUN_E2E) $(E2E_IMAGE):l$(E2E_LAYER) \
		$(DOTNET) test $(ACCEPTANCE_PROJECT) --nologo --filter "$(ACCEPTANCE_FILTER)"

.PHONY: acceptance-usage
acceptance-usage: ## Run the acceptance tests that need a live OPENCODE_GO_API_KEY
acceptance-usage: build-e2e prepare-cache
	@test -n "$$OPENCODE_GO_API_KEY" || { echo "OPENCODE_GO_API_KEY is not set in the environment."; exit 1; }
	$(DOCKER_RUN_E2E) $(E2E_IMAGE):l$(E2E_LAYER) \
		$(DOTNET) test $(ACCEPTANCE_PROJECT) --nologo --filter "$(USAGE_FILTER)"

.PHONY: dependency
dependency: ## Re-validate the facts this module assumes about opencode (opt-in)
dependency: build-e2e prepare-cache
	$(DOCKER_RUN_E2E) $(E2E_IMAGE):l$(E2E_LAYER) \
		$(DOTNET) test $(INTEGRATION_PROJECT) --nologo --filter "$(DEPENDENCY_FILTER)"

.PHONY: e2e
e2e: ## Run the integration and acceptance tiers
e2e: integration acceptance

.PHONY: e2e-shell
e2e-shell: ## Open an interactive shell in the containerised test image
e2e-shell: build-e2e prepare-cache
	$(DOCKER) run --rm --user $(DOCKER_USER) -it \
		-e NUGET_PACKAGES=/tmp/opencode-go-waybar/nuget -e HOME=/tmp/e2e-home \
		-e DOTNET_CLI_HOME=/tmp/e2e-home -e E2E_WORKSPACE=/workspace \
		-v $(CURDIR):/workspace -v $(NUGET_CACHE):/tmp/opencode-go-waybar/nuget \
		--platform=linux/amd64 -w /workspace $(E2E_IMAGE):l$(E2E_LAYER) /bin/bash

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

# The UI targets run on the host toolchain rather than in the dev container: the
# container has no display, and Dockerfile restores only the module and its unit
# tests by name, so it has never heard of the UI project. Keeping them off `test`
# and `build` is deliberate — the pre-commit hook's container path stays exactly
# as it was.
.PHONY: ui-test
ui-test: ## Run the Avalonia UI unit tests on the host
ui-test:
	$(DOTNET) test $(UI_TEST_PROJECT) --nologo

.PHONY: ui-run
ui-run: ## Run the usage window on this machine (ARGS=--rings|--dashboard|--light)
ui-run:
	$(DOTNET) run --project $(UI_PROJECT) -- $(ARGS)

.PHONY: ui-publish
ui-publish: ## Publish the UI binary for $(RUNTIME) into out/ui
ui-publish:
	$(DOTNET) publish $(UI_PROJECT) --configuration $(CONFIGURATION) --runtime $(RUNTIME) \
		--self-contained true --output out/ui/$(RUNTIME)

.PHONY: clean
clean: ## Remove generated build output owned by the current user
clean:
	-$(DOCKER) image rm -f $(IMAGE) $(IMAGE)-prod 2>/dev/null || true
	-$(DOCKER) image rm -f $(E2E_IMAGE):l1 $(E2E_IMAGE):l2 $(E2E_IMAGE):l3 2>/dev/null || true
	-$(DOCKER) rm -f $(IMAGE)-specmatic 2>/dev/null || true
	-$(DOCKER) network rm -f $(TEST_NETWORK) 2>/dev/null || true
	rm -rf out src/OpencodeGoWaybar/bin src/OpencodeGoWaybar/obj \
	       src/OpencodeGoWaybar.Ui/bin src/OpencodeGoWaybar.Ui/obj \
	       tests/OpencodeGoWaybar.Ui.UnitTests/bin tests/OpencodeGoWaybar.Ui.UnitTests/obj \
	       tests/OpencodeGoWaybar.UnitTests/bin tests/OpencodeGoWaybar.UnitTests/obj \
	       tests/OpencodeGoWaybar.IntegrationTests/bin tests/OpencodeGoWaybar.IntegrationTests/obj \
	       tests/OpencodeGoWaybar.AcceptanceTests/bin tests/OpencodeGoWaybar.AcceptanceTests/obj

.PHONY: clean-all
clean-all: ## Remove build output even when owned by root (uses sudo)
clean-all: clean
	-sudo rm -rf src/OpencodeGoWaybar/bin src/OpencodeGoWaybar/obj \
	           tests/OpencodeGoWaybar.UnitTests/bin tests/OpencodeGoWaybar.UnitTests/obj \
	           out

.PHONY: install
install: ## Install the compiled binaries to ~/.local/bin and ~/.local/share
install: publish ui-publish
	install -Dm755 out/$(RUNTIME)/opencode-go-waybar $(HOME)/.local/bin/opencode-go-waybar
	mkdir -p $(HOME)/.local/share/opencode-go-waybar-ui
	cp -r out/ui/$(RUNTIME)/. $(HOME)/.local/share/opencode-go-waybar-ui/
	ln -sf $(HOME)/.local/share/opencode-go-waybar-ui/opencode-go-waybar-ui $(HOME)/.local/bin/opencode-go-waybar-ui
	@echo "Installation complete! Ensure $(HOME)/.local/bin is in your PATH."

.PHONY: uninstall
uninstall: ## Remove the installed application from your system
uninstall:
	rm -f $(HOME)/.local/bin/opencode-go-waybar
	rm -f $(HOME)/.local/bin/opencode-go-waybar-ui
	rm -rf $(HOME)/.local/share/opencode-go-waybar-ui
	@echo "Uninstalled successfully."