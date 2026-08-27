-- Neovim ACP client for e2e layer 2.
--
-- CodeCompanion.nvim ships a built-in `opencode` ACP adapter that runs
-- {"opencode", "acp"} exactly as https://opencode.ai/docs/acp/ documents, so
-- this only overrides the command path to point at the binary under test.
--
--   nvim --headless -u /opt/nvim-e2e/init.lua -c 'lua E2eStartAcp()'
--
-- Prints one JSON object on stdout for the E2E suite to assert on:
--   {"spawned":true,"pids":[123],"adapter":"opencode"}

vim.opt.packpath:append('/opt/nvim-e2e')
vim.opt.runtimepath:append('/opt/nvim-e2e/pack/e2e/start/plenary.nvim')
vim.opt.runtimepath:append('/opt/nvim-e2e/pack/e2e/start/codecompanion.nvim')

local opencode_bin = vim.env.OPENCODE_BIN or 'opencode'

-- The agent spawns before authentication is attempted, and without credentials
-- CodeCompanion then reports the failed initialize. Its reporting path calls
-- nvim_echo from a fast event context, which is fatal in --headless. Silencing
-- notifications keeps that expected failure from tearing down the probe.
vim.notify = function() end
vim.notify_once = function() end

local loaded, codecompanion = pcall(require, 'codecompanion')

if not loaded then
    print(vim.json.encode({
        spawned = false,
        error = 'codecompanion.nvim failed to load: ' .. tostring(codecompanion),
    }))
    vim.cmd('cquit 1')
end

codecompanion.setup({
    adapters = {
        acp = {
            opencode = function()
                return require('codecompanion.adapters').extend('opencode', {
                    commands = {
                        default = { opencode_bin, 'acp' },
                    },
                })
            end,
        },
    },
    strategies = {
        chat = { adapter = 'opencode' },
    },
})

-- Every pid whose cmdline mentions `opencode` and `acp` — how an
-- editor-spawned ACP agent appears in the process table.
local function acp_pids()
    local pids = {}

    for _, entry in ipairs(vim.fn.readdir('/proc')) do
        if entry:match('^%d+$') then
            local ok, cmdline = pcall(vim.fn.readfile, '/proc/' .. entry .. '/cmdline', 'b')

            if ok and cmdline and cmdline[1] then
                local joined = cmdline[1]:gsub('%z', ' ')

                if joined:match('opencode') and joined:match('acp') then
                    table.insert(pids, tonumber(entry))
                end
            end
        end
    end

    return pids
end

local function wait_for_agent(attempts)
    for _ = 1, attempts do
        local pids = acp_pids()

        if #pids > 0 then
            return pids
        end

        vim.wait(500)
    end

    return {}
end

function E2eStartAcp()
    local errors = {}

    local opened, open_error = pcall(function()
        vim.cmd('CodeCompanionChat')
    end)

    if not opened then
        table.insert(errors, 'CodeCompanionChat: ' .. tostring(open_error))
    end

    local pids = wait_for_agent(10)

    -- The adapter connects lazily. If opening the chat alone did not spawn the
    -- agent, submitting a prompt does — the request fails without credentials,
    -- but the agent process is started before authentication is attempted.
    if #pids == 0 then
        local submitted, submit_error = pcall(function()
            local chat = require('codecompanion.strategies.chat').last_chat()

            if not chat then
                error('no chat buffer was created')
            end

            chat:add_buf_message({ role = 'user', content = 'hello from the e2e probe' })
            chat:submit()
        end)

        if not submitted then
            table.insert(errors, 'submit: ' .. tostring(submit_error))
        end

        pids = wait_for_agent(40)
    end

    print(vim.json.encode({
        spawned = #pids > 0,
        pids = pids,
        adapter = 'opencode',
        command = opencode_bin .. ' acp',
        errors = errors,
    }))

    -- Hold the agent so the Waybar module can observe it in the process table.
    -- Wrapped because CodeCompanion's own async callbacks can raise here once
    -- the unauthenticated session fails; the agent stays up regardless.
    pcall(vim.wait, tonumber(vim.env.E2E_HOLD_MS or '3000'))
    vim.cmd('qall!')
end
