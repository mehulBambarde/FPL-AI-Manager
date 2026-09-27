namespace FPL.AI.Manager.Console;

public class CompletedCourse
{
    // foreach (var prompt in userPrompts)
// {
//     Console.WriteLine($"\nUser: {prompt}");
//     Console.Write("Agent: ");
//     
//     var response = await agent.RunAsync(prompt, sesh);
//     Console.WriteLine(response);
//     
//     Console.WriteLine("\n" + new string('-', 50));
// }
// AgentSession sesh = await agent.CreateSessionAsync();
// Console.WriteLine("Your FPL assistant is Live, Type 'exit' to quit \n");
//
// while (true)
// {
//     Console.Write("User: ");
//     string? input = Console.ReadLine();
//
//     AgentResponse response = await agent.RunAsync(input, sesh);
//     Console.WriteLine($"Agent: {response.Text}\n");
// }

// var question = "What is Fantasy Premier League and how does captaincy work?";
// Console.WriteLine($"User : {question}");
//
// await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(question))
// {
//     Console.WriteLine(update.Text);
// }
// var client = new AIProjectClient(
//     new Uri(baseUrl!), 
//     new AzureCliCredential());
//     
// var chatClient = client.GetAzureOpenAIChatClient(model).AsIChatClient();
//
// AIAgent  agent = chatClient.AsAIAgent(
//     name: "TestAgent",
//     instructions: "You are a Test Agent. Your answers must be concise, professional, and limited strictly");
//     
// Console.WriteLine($"Agent '{agent.Name}' is online.\n");
//
// var question = "What is Fantasy Premier League and how does captaincy work?";
// Console.WriteLine($"User : {question}");
// Console.WriteLine(await agent.RunAsync(question));

// Create the Agent using MAF
// AIAgent agent = new AIProjectClient(
//         new Uri(baseUrl!),
//         new AzureCliCredential())
//     .AsAIAgent(
//         model: model!,
//         name: "FPL Assistant",
//         instructions: "You are a helpful assistant for understanding Fantasy Premier League. Keep your answers brief",
//         tools:
//         [
//             AIFunctionFactory.Create(FplService.GetAllPlayersAsync),
//             AIFunctionFactory.Create(FplService.GetFixturesAsync),
//             AIFunctionFactory.Create(FplService.GetPlayerSummaryAsync),
//             AIFunctionFactory.Create(FplService.GetLiveGameweekDataAsync),
//             AIFunctionFactory.Create(FplService.GetTopOwnedPlayersAsync)
//         ]);

}