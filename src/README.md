## FlowSynx Telegram Plugin

The Telegram Plugin is a pre-packaged, plug-and-play integration component for the FlowSynx engine. It enables sending Telegram messages through customizable parameters such as chat ID and message text, and is designed for FlowSynx no-code/low-code automation workflows.

This plugin is automatically installed by the FlowSynx engine when selected within the platform. It is not intended for manual installation or standalone developer usage outside the FlowSynx environment.

---

## Purpose

This plugin allows FlowSynx users to send Telegram messages from within workflows without writing code. Once installed, it appears as a connector in the platform workflow builder, enabling seamless integration into automated notification or messaging processes.

---

## Plugin Specifications

To function properly, the plugin requires the following specification:

- `BotToken`: The token for your Telegram bot (see below).

---

## Getting Started

### Create a Telegram Bot

To send messages via Telegram, you must first create a bot:

1. Open Telegram and search for [@BotFather](https://t.me/BotFather).
2. Start a conversation and send `/newbot`.
3. Follow the instructions to:
   - Choose a name (e.g., `FlowSynx Notifier`)
   - Choose a unique username ending in `bot` (e.g., `flowsynx_notifier_bot`)
4. You will receive a **Bot Token** (e.g., `123456789:ABCDefGhIJKlmnoPQRstuVWXyz`).

Use this token in the plugin `BotToken` specification.

---

## Input Parameters

When executing the plugin, provide the following parameters in the input dictionary:

- `chatId` (string): The target chat ID (user ID, group ID, or channel username).
- `message` (string): The text message to be sent. Supports Markdown or plain text.

### Example Input

```json
{
  "chatId": "@your_channel_name",
  "message": "Your workflow has completed successfully."
}
```

---

## How to Get `chatId`

### For Private Users

- The user must start a chat with the bot first.
- Then visit this URL in a browser: `https://api.telegram.org/bot<YourBotToken>/getUpdates`
- Look for `"chat":{"id":<number>}` in the JSON response.

### For Public Channels

- Add the bot as an admin to the channel.
- Use the `@channel_username` as `chatId`.

### For Groups

- Add the bot to the group.
- Send a message in the group.
- Retrieve the chat ID from `/getUpdates`. It will usually be a negative number (e.g., `-123456789`).

---

## Example Use Case in FlowSynx

1. Add the Telegram plugin to your workflow.
2. Set the `BotToken` in the plugin specifications.
3. In the operation input, provide `chatId` and `message`.
4. Choose the `send` operation to trigger the Telegram message.

---

## Debugging Tips

- **Bad Request, chat not found**: Make sure the `chatId` is valid and accessible to the bot.
- **Forbidden**: The user or group has not accepted/started the bot.
- **No messages received**: Ensure the bot has the right permissions and is in the target group/channel.
- **No updates from **: Send a message to the bot first to generate data.

---

## Security Notes

- Do not share your `BotToken` publicly.
- FlowSynx secures all sensitive configuration internally.
- Only authorized users within the platform can modify bot configurations.

---

## License

© FlowSynx. All rights reserved.