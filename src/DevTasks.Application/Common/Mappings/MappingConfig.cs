using DevTasks.Application.DTOs;
using DevTasks.Domain.Entities;
using Mapster;

namespace DevTasks.Application.Common.Mappings
{
    public class MappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // TaskItem <-> TaskItemDto (both directions)
            config.NewConfig<TaskItem, TaskItemDto>().TwoWays();

            // CreateTaskDto -> TaskItem
            config.NewConfig<CreateTaskDto, TaskItem>();

            // UpdateTaskDto -> TaskItem
            config.NewConfig<UpdateTaskDto, TaskItem>();
        }
    }
}