using System;
using System.Text.Json;
using AutoMapper;
using CommandsService.Data;
using CommandsService.Dtos;
using CommandsService.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CommandsService.EventProcessing
{
    public class EventProcessor : IEventProcessor
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public EventProcessor(IServiceScopeFactory scopeFactory, AutoMapper.IMapper mapper)
        {
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }

        public void ProcessEvent(string message)
        {
            var eventType = DetermineEvent(message);

            switch (eventType)
            {
                case EventType.PlatformPublished:
                    AddPlatform(message);
                    break;
                default:
                    break;
            }
        }

        private EventType DetermineEvent(string notificationMessage)
        {
            Console.WriteLine($"--> Determining Event");

            var eventType = JsonSerializer.Deserialize<GenericEventDto>(notificationMessage);

            switch (eventType.Event)
            {
                case "Platform_Published":
                    Console.WriteLine($"--> Platform Published Event Detected");
                    return EventType.PlatformPublished;
                default:
                    Console.WriteLine($"--> Could not determine the event type");
                    return EventType.Undetermined;
            }
        }

        private void AddPlatform(string platformPublishedMessage)
        {
            using (var scoped = _scopeFactory.CreateScope())
            {
                var repo = scoped.ServiceProvider.GetRequiredService<ICommandRepo>();

                var platformPublishedDto = JsonSerializer.Deserialize<PlatfromPublishedDto>(platformPublishedMessage);

                try
                {
                    var plat = _mapper.Map<Platform>(platformPublishedDto);
                    if (!repo.ExternalPlatformExist(plat.ExternalID))
                    {
                        repo.CreatePlatform(plat);
                        repo.SaveChanges();
                        Console.WriteLine($"--> Platform added!");
                    }
                    else
                    {
                        Console.WriteLine($"--> Platform already exists...");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"--> Could not add Platform to DB {e.Message}");
                }
            }
        }
    }

    enum EventType
    {
        PlatformPublished,
        Undetermined
    }
}