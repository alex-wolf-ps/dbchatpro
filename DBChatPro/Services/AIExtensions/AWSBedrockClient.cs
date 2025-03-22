using System.Runtime.CompilerServices;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Azure;
using Microsoft.Extensions.AI;

public sealed class AWSBedrockClient(
    IAmazonBedrockRuntime bedrockClient, string modelId) : IChatClient
{
    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var bedRockMessages = new List<Message>();

        // Convert MEAI messages into Bedrock messages
        foreach (var message in messages)
        {
            bedRockMessages.Add(new Message
            {
                Role = ConversationRole.User,
                Content = new List<ContentBlock> { new ContentBlock { Text = message.Text } }
            });
        }

        // Create a request with the model ID and messages
        var request = new ConverseRequest
        {
            ModelId = modelId,
            Messages = bedRockMessages
        };

        try
        {
            // Send the request to the Bedrock Runtime and wait for the result.
            var response = await bedrockClient.ConverseAsync(request);

            // Convert the result to MEAI types
            return new([new ChatMessage(ChatRole.Assistant, response.Output.Message.Content[0].Text)]);
        }
        catch (AmazonBedrockRuntimeException e)
        {
            // Convert the result to MEAI types
            return new([new ChatMessage(ChatRole.Assistant, $"ERROR: Can't invoke '{modelId}'. Reason: {e.Message}")]);
        }
    }

    // These aren't needed by the app
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        throw new NotImplementedException();
    }

    // These aren't needed by the app
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}