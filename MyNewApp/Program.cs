using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Rewrite;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseRewriter(new RewriteOptions().AddRedirect("tasks/(.*)", "todos/$1"));
app.Use(async (context, next) => {
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Started");
    await next(context);
    Console.WriteLine($"[{context.Request.Method} {context.Request.Path} {DateTime.UtcNow}] Ended");
});

var todos = new List<Todo>();

app.MapGet("/", () => "Hello World!");

app.MapGet("/todos", () => todos);

app.MapGet("/todos/{id}", Results<Ok<Todo>, NotFound>(int id) => {
    var targetTodo = todos.SingleOrDefault(t => id == t.Id);

    return targetTodo is null ? TypedResults.NotFound() : TypedResults.Ok(targetTodo);
});

app.MapPost("/todos", (Todo task) => {
    todos.Add(task);

    return TypedResults.Created("/todos/{id}", task);
}).AddEndpointFilter(async (context, next) => {
    var taskArgument = context.GetArgument<Todo>(0);
    var errors = new Dictionary<string, string[]>();

    if (taskArgument.DueDate < DateTime.UtcNow) {
        errors.Add(nameof(Todo.DueDate), ["Cannot have due date in the past."]);
    }

    if (taskArgument.isCompleted) {
        errors.Add(nameof(Todo.isCompleted), ["Cannot add completed Todo"]);
    }

    if (errors.Count > 0) {
        return Results.ValidationProblem(errors);
    }

    return await next(context);
});

app.MapDelete("/todos/{id}", (int id) => {
    todos.RemoveAll(t => id == t.Id);

    return TypedResults.NoContent();
});

app.Run();

public record Todo(int Id, string Name, DateTime DueDate, bool isCompleted) {}