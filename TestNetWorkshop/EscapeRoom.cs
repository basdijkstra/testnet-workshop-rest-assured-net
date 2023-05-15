using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using NUnit.Framework;
using NUnit.Framework.Internal;
using RestAssured.Request.Builders;
using RestAssured.Response;
using RestAssured.Response.Deserialization;
using TestNetWorkshop.Models;
using static RestAssured.Dsl;

namespace TestNetWorkshop
{
    public class EscapeRoom
    {
        private RequestSpecification requestSpec;

        [SetUp]
        public void CreateRequestSpecification()
        {
            this.requestSpec = new RequestSpecBuilder()
                .WithScheme("https")
                .WithHostName("ta-workshop.nl")
                .WithHeader("apikey", "b1a0f554-8990-46e1-9ef0-bf6c01f47995")
                .WithRelaxedHttpsValidation()
                .Build();
        }

        [Test]
        public void EscapeTheEscapeRoom()
        {
            CreateSession();
            SetSessionStatusTo("PLAYING");

            StartResponse startResponse = GetStartResponse();
            TowersResponse towersResponse = GetTowersResponse();

            string solution1Header = UnlockSafeUsing(startResponse);

            HttpResponseMessage response = SolveTowersPuzzleUsing(solution1Header, towersResponse);

            response.Headers.TryGetValues("solution2", out var values);
            string solution2Header = values!.First();

            int specialistNumber = GetSpecialistNumberFrom(response);

            long escapeCode = GetEscapeCodeFrom(specialistNumber, solution2Header);

            EscapeFromTheEscapeRoomUsing(escapeCode);
        }

        private void CreateSession()
        {
            Given()
                .Spec(this.requestSpec)
                .QueryParam("gebruikersnaam", "Bas Dijkstra")
                .QueryParam("roomId", 1)
                .When()
                .Get("/session/create")
                .Then()
                .StatusCode(208);
        }

        private void SetSessionStatusTo(string status)
        {
            Given()
                .Spec(this.requestSpec)
                .QueryParam("apikey", "b1a0f554-8990-46e1-9ef0-bf6c01f47995")
                .QueryParam("status", status)
                .When()
                .Put("/session/status")
                .Then()
                .StatusCode(200);
        }

        private StartResponse GetStartResponse()
        {
            return (StartResponse)Given()
                .Spec(this.requestSpec)
                .When()
                .Get("/duo/start")
                .Then()
                .DeserializeTo(typeof(StartResponse), DeserializeAs.Json);
        }

        private TowersResponse GetTowersResponse()
        {
            return (TowersResponse)Given()
                .Spec(this.requestSpec)
                .When()
                .Get("/duo/towers")
                .Then()
                .DeserializeTo(typeof(TowersResponse), DeserializeAs.Json);
        }

        private string UnlockSafeUsing(StartResponse startResponse)
        {
            return Given()
                .Spec(this.requestSpec)
                .QueryParam("solution", GetSumAnswerFrom(startResponse))
                .When()
                .Get("/duo/combination_lock")
                .Then()
                .StatusCode(200)
                .And()
                .Extract().Header("solution1");
        }

        private HttpResponseMessage SolveTowersPuzzleUsing(string solution1Header, TowersResponse towersResponse)
        {
            return Given()
                .Spec(this.requestSpec)
                .Header("solution1", solution1Header)
                .Body(GetTowersOrderFrom(towersResponse))
                .When()
                .Post("/duo/towers")
                .Then()
                .StatusCode(200)
                .And()
                .Extract().Response();
        }

        private int GetSpecialistNumberFrom(HttpResponseMessage response)
        {
            string responseBody = response.Content.ReadAsStringAsync().Result;

            int specialistNumber = 0;

            if (!responseBody.Contains("Playwright"))
            {
                specialistNumber = 587426; //Jurian  
            }
            else if (!responseBody.Contains("Postman"))
            {
                specialistNumber = 32843; // David  
            }
            else if (!responseBody.Contains("Cypress"))
            {
                specialistNumber = 7346337; //Reinder  
            }
            else if (!responseBody.Contains("Python"))
            {
                specialistNumber = 6278456; //Martijn  
            }
            else
            {
                specialistNumber = 527786; //Jarsto  
            }

            return specialistNumber;
        }

        private long GetEscapeCodeFrom(int specialistNumber, string solution2Header)
        {
            return (long)Given()
                .Spec(this.requestSpec)
                .Header("solution2", solution2Header)
                .PathParam("specialist", specialistNumber)
                .When()
                .Get("/duo/tool_support/call_specialist/{{specialist}}")
                .Then()
                .StatusCode(200)
                .And()
                .Extract().Body("$.escapecode", ExtractAs.Json);
        }

        private void EscapeFromTheEscapeRoomUsing(long escapeCode)
        {
            Given()
                .Spec(this.requestSpec)
                .PathParam("escapeCode", escapeCode)
                .When()
                .Delete("/duo/remove_lock/{{escapeCode}}")
                .Then()
                .StatusCode(200)
                .And()
                .Body("$.response", NHamcrest.Is.EqualTo("You have escaped!"), VerifyAs.Json);
        }

        private TowersOrder GetTowersOrderFrom(TowersResponse response)
        {
            if (response.towers!.alphabetic)
            {
                return new TowersOrder
                {
                    aKerk = 1,
                    academieGebouw = 0,
                    martini = 2,
                    nieuweKerk = 3,
                    stJozefKerk = 4,
                };
            }

            if (response.towers.height)
            {
                return new TowersOrder
                {
                    aKerk = 3,
                    academieGebouw = 1,
                    martini = 4,
                    nieuweKerk = 0,
                    stJozefKerk = 2,
                };
            }

            return new TowersOrder
            {
                aKerk = 1,
                academieGebouw = 4,
                martini = 0,
                nieuweKerk = 2,
                stJozefKerk = 3,
            };
        }

        private int GetSumAnswerFrom(StartResponse startResponse)
        {
            if (startResponse.add)
            {
                return startResponse.sum.number1 + startResponse.sum.number2;
            }

            if (startResponse.subtract)
            {
                return startResponse.sum.number1 - startResponse.sum.number2;
            }

            if (startResponse.divide)
            {
                return startResponse.sum.number1 / startResponse.sum.number2;
            }

            if (startResponse.multiply)
            {
                return startResponse.sum.number1 * startResponse.sum.number2;
            }

            return 0;
        }
    }
}
