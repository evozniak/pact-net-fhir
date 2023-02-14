// See https://aka.ms/new-console-template for more information
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using PactNet;
using PactNet.Matchers;

Console.WriteLine("Hello, World!");

IPactBuilderV3 pactBuilder;

var pact = Pact.V3("IOB PICS Consumer", "IOB PICS Integration", new PactConfig { PactDir = "./pacts" });

// or specify custom log and pact directories
// pact = Pact.V3("Something API Consumer", "Something API", new PactConfig
// {
//     PactDir = $"./pacts/"
// });

// Initialize Rust backend
pactBuilder = pact.WithHttpInteractions(1238, PactNet.Models.IPAddress.Loopback);

string request = """
{
    "resourceType": "Bundle",
    "type": "collection",
    "entry": [{
            "resource": {
                "resourceType": "DocumentReference",
                "content": [{
                        "attachment": {
                            "data": "sampledata"
                        }
                    }
                ]
            }
        }
    ]
}
""";

pactBuilder
            .UponReceiving("PICS document outbound")
                .Given("A POST request with a Bundle, DocumentReference and Attachment'")
                .WithRequest(HttpMethod.Post, "/share/toolkit/fhirtohl7/R4")
                .WithHeader("Authorization", Match.Include("Bearer"))
                .WithHeader("Accept", "application/json")
                .WithJsonBody(new
                {
                    resourceType = Match.Include("Bundle"),
                    type = Match.Include("collection"),
                    entry = new[] {
                    new {
                        resource = new
                        {
                            resourceType = Match.Include("DocumentReference"),
                            content = new[] {
                            new {
                                attachment = new
                                {
                                    data = Match.Type("string")
                                }
                            }}
                        },
                    }}
                })
            .WillRespond()
                .WithStatus(HttpStatusCode.OK)
                .WithHeader("Content-Type", "application/json; charset=utf-8")
                .WithJsonBody(new
                {
                    message = "ok, processed",
                });

await pactBuilder.VerifyAsync(async ctx =>
{
    var client = new HttpClient { BaseAddress = ctx.MockServerUri };
    client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer xxxx");
    var response = await client.PostAsync("/share/toolkit/fhirtohl7/R4", new StringContent(request, MediaTypeHeaderValue.Parse("application/json")));
    Console.WriteLine("ok...");
    Console.WriteLine(await response.Content.ReadAsStringAsync());
});