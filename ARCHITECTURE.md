# Arquitectura del Proyecto: Clean Architecture + CQRS + Vertical Slices (Monolito Simplificado)

Este documento define las directrices arquitectónicas, la estructura de directorios y los patrones de diseño para el desarrollo del proyecto **Test-servidores**.

---

## 1. Filosofía de Diseño

El objetivo principal es mantener la **simplicidad y la velocidad de desarrollo** sin sacrificar la **separación de responsabilidades** y la **mantenibilidad**. Para lograr esto, combinamos tres enfoques:

1. **Clean Architecture (Dependency Rule)**: El núcleo del negocio (**Domain**) no depende de nada. La capa de **Application** depende del Domain. La capa de **Infrastructure** (Data, API, servicios externos) depende de Application y Domain.
2. **Single API Project (Monolito de un solo proyecto)**: En lugar de separar en múltiples proyectos `.csproj` (lo que añade complejidad de compilación y referencias), organizamos las capas dentro del mismo proyecto `Api.csproj` utilizando **carpetas y Namespaces**.
3. **CQRS con Vertical Slices (Features)**: In la capa de aplicación, en lugar de organizar el código por tipo de archivo técnico (Controllers, Services, Repositories), lo agrupamos por **Características de Negocio (Features)**. Cada feature es un "rebanada vertical" (Vertical Slice) que contiene su propia lógica de comandos, consultas, validaciones y respuestas.

---

## 2. Estructura de Directorios

El proyecto `Api` se organiza de la siguiente manera:

```text
Api/
│
├── Domain/                         # Capa de Dominio (Sin dependencias externas)
│   ├── Common/                     # Clases base (Entity, ValueObject, AggregateRoot)
│   ├── Entities/                   # Entidades del negocio (Ej. Product.cs, Order.cs)
│   ├── ValueObjects/               # Objetos de Valor (Ej. Money.cs, Address.cs)
│   └── Exceptions/                 # Excepciones de negocio (Ej. InsufficientStockException.cs)
│
├── Application/                    # Capa de Aplicación (Casos de uso y reglas de negocio)
│   ├── Common/                     # Interfaces, comportamientos de MediatR (Pipeline Behaviors), Result Pattern
│   └── Features/                   # Funcionalidades del sistema agrupadas por dominio/feature
│       └── Products/               # Módulo de Productos
│           ├── CreateProduct/      # Vertical Slice para Crear Producto (CQRS Command)
│           │   ├── CreateProductCommand.cs
│           │   ├── CreateProductCommandHandler.cs
│           │   ├── CreateProductCommandValidator.cs
│           │   └── CreateProductResponse.cs
│           └── GetProduct/         # Vertical Slice para Obtener Producto (CQRS Query)
│               ├── GetProductQuery.cs
│               ├── GetProductQueryHandler.cs
│               └── GetProductResponse.cs
│
├── Infrastructure/                 # Capa de Infraestructura (Implementaciones de tecnología)
│   ├── Data/                       # Base de datos (EF Core, Migraciones)
│   │   ├── Configurations/         # Mapeos Fluent API (Ej. ProductConfiguration.cs)
│   │   └── ApplicationDbContext.cs # Contexto de EF Core
│   └── Services/                   # Comunicaciones externas (Email, Filesystem, etc.)
│
├── Controllers/                    # Controladores Web API (Punto de entrada HTTP)
│   └── ProductsController.cs       # Mapea endpoints HTTP a comandos/consultas de MediatR
│
├── Program.cs                      # Configuración de dependencias y pipeline de ASP.NET
└── Api.csproj                      # Configuración de proyecto de .NET 10
```

---

## 3. Capas en Detalle y Patrones

### A. Capa de Dominio (`Domain/`)
Contiene los modelos del negocio. Es pura, no tiene referencias a EF Core, bases de datos o frameworks web.
*   **Entidades (`Entities/`)**: Objetos con identidad única (ID) que mutan su estado a través de métodos de negocio, nunca exponiendo setters públicos arbitrarios.
*   **Objetos de Valor (`ValueObjects/`)**: Objetos sin identidad propia, definidos únicamente por sus atributos. Son inmutables. En C# se implementan idealmente usando `record`.
*   **Reglas de negocio**: La lógica de negocio pesada debe estar dentro de las entidades del dominio, no en los controladores ni en los handlers.

### B. Capa de Aplicación (`Application/`)
Implementa los casos de uso del sistema.
*   **CQRS (Command Query Responsibility Segregation)**:
    *   **Commands (Comandos)**: Operaciones que mutan el estado del sistema. No deben retornar entidades de dominio directas (retornar un ID, un DTO simple o un resultado de éxito).
    *   **Queries (Consultas)**: Operaciones de solo lectura. No deben mutar el estado. Optimizadas para velocidad.
*   **MediatR**: Desacopla los controladores HTTP de los casos de uso. El controlador solo despacha un mensaje a MediatR, y este se encarga de ejecutar el Handler correspondiente.
*   **Result Pattern**: En lugar de lanzar excepciones para flujos de control o errores de validación de negocio (lo cual es costoso), los Handlers devuelven un tipo genérico `Result<T>` o `Result` indicando éxito o fracaso con detalles del error.
*   **Pipeline Behaviors**: Filtros transversales para MediatR que se ejecutan automáticamente en cada solicitud (ej. Validación automática con FluentValidation antes de que el handler procese el comando).

### C. Capa de Infraestructura (`Infrastructure/`)
Contiene los detalles técnicos y adaptadores externos.
*   **Data (EF Core)**:
    *   `ApplicationDbContext` hereda de `DbContext`.
    *   Configuraciones Fluent API en clases separadas (implementando `IEntityTypeConfiguration<T>`) para evitar ensuciar el DbContext.
*   **Services**: Clientes HTTP para APIs externas, generadores de tokens JWT, almacenamiento de archivos, etc.

### D. Capa de Entrada (`Controllers/` o `Endpoints/`)
*   Se encarga de recibir peticiones HTTP, mapear la entrada a un comando o consulta de MediatR, enviarla a través del Mediator y retornar un código HTTP apropiado (ej. `200 OK`, `201 Created`, `400 BadRequest`, `404 NotFound`) basado en el objeto `Result` devuelto.

---

## 4. Ejemplos Prácticos de Código

A continuación se presentan plantillas estándar para implementar la arquitectura.

### Ejemplo 1: Objeto de Valor (`Domain/ValueObjects/Money.cs`)
```csharp
namespace Api.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("El monto no puede ser negativo.", nameof(amount));
            
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "USD")
    {
        return new Money(amount, currency);
    }
}
```

### Ejemplo 2: Entidad (`Domain/Entities/Product.cs`)
```csharp
using Api.Domain.ValueObjects;

namespace Api.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Money Price { get; private set; } = null!;
    public int Stock { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Constructor requerido por EF Core
    private Product() { }

    public Product(Guid id, string name, Money price, int stock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto es requerido.");

        Id = id;
        Name = name;
        Price = price;
        Stock = stock;
        CreatedAt = DateTime.UtcNow;
    }

    // Regla de Negocio en el Dominio (Encapsulación)
    public void UpdatePrice(Money newPrice)
    {
        Price = newPrice;
    }

    public void ReduceStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("La cantidad debe ser mayor a cero.");
        if (Stock < quantity)
            throw new InvalidOperationException("Stock insuficiente para realizar la operación.");

        Stock -= quantity;
    }
}
```

### Ejemplo 3: El Patrón Result (`Application/Common/Result.cs`)
```csharp
namespace Api.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
            throw new ArgumentException("Estado de error inválido", nameof(error));

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default!, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue _value;

    protected internal Result(TValue value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value
        : throw new InvalidOperationException("No se puede acceder al valor de un resultado fallido.");
}

public record Error(string Code, string Message)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}
```

### Ejemplo 4: Vertical Slice - Comando de Creación (`Application/Features/Products/CreateProduct/...`)

#### 1. Comando (`CreateProductCommand.cs`)
```csharp
using Api.Application.Common;
using MediatR;

namespace Api.Application.Features.Products.CreateProduct;

public record CreateProductCommand(
    string Name, 
    decimal PriceAmount, 
    string Currency, 
    int Stock
) : IRequest<Result<Guid>>;
```

#### 2. Validador (`CreateProductCommandValidator.cs`)
```csharp
using FluentValidation;

namespace Api.Application.Features.Products.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido.")
            .MaximumLength(150).WithMessage("El nombre no debe superar los 150 caracteres.");

        RuleFor(x => x.PriceAmount)
            .GreaterThan(0).WithMessage("El precio debe ser mayor a 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("La moneda es requerida.")
            .Length(3).WithMessage("La moneda debe ser un código ISO de 3 letras (Ej. USD, EUR).");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");
    }
}
```

#### 3. Handler (`CreateProductCommandHandler.cs`)
```csharp
using Api.Application.Common;
using Api.Domain.Entities;
using Api.Domain.ValueObjects;
using Api.Infrastructure.Data;
using MediatR;

namespace Api.Application.Features.Products.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Result<Guid>>
{
    private readonly ApplicationDbContext _context;

    public CreateProductCommandHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Creación del objeto de valor
        Money price;
        try
        {
            price = Money.Create(request.PriceAmount, request.Currency);
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<Guid>(new Error("Product.InvalidPrice", ex.Message));
        }

        // 2. Creación de la entidad
        var product = new Product(
            Guid.NewGuid(),
            request.Name,
            price,
            request.Stock
        );

        // 3. Persistencia
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Retornar resultado de éxito con el ID del producto creado
        return Result.Success(product.Id);
    }
}
```

### Ejemplo 5: Infraestructura - Configuración EF Core (`Infrastructure/Data/...`)

#### 1. Configuración de Entidad (`Infrastructure/Data/Configurations/ProductConfiguration.cs`)
```csharp
using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        // Mapeo del Objeto de Valor Money (Owned Entity)
        builder.OwnsOne(p => p.Price, priceBuilder =>
        {
            priceBuilder.Property(m => m.Amount)
                .HasColumnName("PriceAmount")
                .HasPrecision(18, 2)
                .IsRequired();

            priceBuilder.Property(m => m.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(p => p.Stock)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();
    }
}
```

#### 2. Contexto de Base de Datos (`Infrastructure/Data/ApplicationDbContext.cs`)
```csharp
using Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Api.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aplica todas las configuraciones que implementan IEntityTypeConfiguration en este ensamblado
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}
```

### Ejemplo 6: Entrada - Controlador de API (`Controllers/ProductsController.cs`)
```csharp
using Api.Application.Features.Products.CreateProduct;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command, 
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            // Mapea el error a una respuesta HTTP BadRequest con detalles
            return BadRequest(new { 
                Error = result.Error.Code, 
                Message = result.Error.Message 
            });
        }

        return CreatedAtAction(nameof(Create), new { id = result.Value }, result.Value);
    }
}
```

---

## 5. Middleware de Validación Automática (Pipeline Behavior)

Para evitar validar manualmente en cada Handler, se registra un `IPipelineBehavior` de MediatR que ejecuta todos los validadores de FluentValidation registrados antes de que llegue al Handler. Si hay errores de validación, lanza una excepción de validación personalizada que es capturada por el manejador global de excepciones del servidor API.

```csharp
// Application/Common/ValidationBehavior.cs
using FluentValidation;
using MediatR;

namespace Api.Application.Common;

public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        return await next();
    }
}
```

---

## 6. Registro de Servicios en `Program.cs`

En .NET 10, puedes agregar todas las dependencias necesarias de la siguiente manera:

```csharp
using Api.Application.Common;
using Api.Infrastructure.Data;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Agregar Controladores
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. Registrar EF Core (Ejemplo con PostgreSQL o SQLite para pruebas)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

// 3. Registrar MediatR y el Pipeline de Validación
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// 4. Registrar todos los validadores de FluentValidation del proyecto
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

---

## 7. Ventajas Clave de este Enfoque

1. **Un solo proyecto para compilar:** Menor sobrecarga de MSBuild, mejor rendimiento del IDE, menor complejidad para configurar migraciones.
2. **Cohesión alta, bajo acoplamiento:** Todo lo relacionado a una Feature (Command, Query, Validador, DTO) vive en la misma carpeta física. Es sumamente fácil de entender y de modificar sin saltar entre 4 proyectos diferentes.
3. **El Dominio es Rey:** Reglas de negocio bien encapsuladas en entidades y protegidas por objetos de valor que previenen estados de datos corruptos.
4. **Validaciones limpias:** Las validaciones de datos (tipo de dato, longitud) se manejan antes de llegar al handler. La lógica en el handler solo se preocupa por flujos del negocio.

---

## 8. Soluciones Implementadas

### A. Dominio de Usuario (`Domain/`)
Hemos construido la entidad de negocio `Usuario` utilizando el objeto de valor `Email` para asegurar la consistencia de los datos del correo.

#### 1. Objeto de Valor `Email` (`Domain/ValueObjects/Email.cs`)
```csharp
using System.Text.RegularExpressions;

namespace Api.Domain.ValueObjects;

public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El correo electrónico no puede estar vacío.", nameof(value));

        if (!EmailRegex.IsMatch(value))
            throw new ArgumentException("El formato del correo electrónico es inválido.", nameof(value));

        Value = value.ToLowerInvariant();
    }

    public static Email Create(string value)
    {
        return new Email(value);
    }

    public override string ToString() => Value;
}
```

#### 2. Entidad `Usuario` (`Domain/Entities/Usuario.cs`)
```csharp
using Api.Domain.ValueObjects;

namespace Api.Domain.Entities;

public class Usuario
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string Apellido { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;

    // Constructor requerido por EF Core
    private Usuario() { }

    public Usuario(Guid id, string nombre, string apellido, Email email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre es requerido.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(apellido))
            throw new ArgumentException("El apellido es requerido.", nameof(apellido));

        ArgumentNullException.ThrowIfNull(email, nameof(email));

        Id = id;
        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }

    public void UpdateInformation(string nombre, string apellido, Email email)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede estar vacío.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(apellido))
            throw new ArgumentException("El apellido no puede estar vacío.", nameof(apellido));

        ArgumentNullException.ThrowIfNull(email, nameof(email));

        Nombre = nombre;
        Apellido = apellido;
        Email = email;
    }
}
```

