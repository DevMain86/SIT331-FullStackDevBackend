using System;
using System.Collections.Generic;
using System.Linq;
using FastMember;
using Npgsql;

// These are the DTO/entity classes used across controllers and persistence.
namespace robot_controller_api
{
    public class RobotCommand
    {
        // Primary key
        public int Id { get; set; }

        // Command name (e.g., "MOVE", "LEFT")
        public string Name { get; set; } = string.Empty;

        // Whether this command causes movement
        public bool IsMoveCommand { get; set; }

        // Timestamps for auditing
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Optional description (nullable because DB may contain NULL)
        public string? Description { get; set; }

        // Parameterless constructor required by some serializers and FastMember
        public RobotCommand() { }

        // Convenience constructor used when creating instances in code/tests
        public RobotCommand(int id, string name, bool isMove, DateTime created, DateTime modified, string? desc)
        {
            Id = id;
            Name = name;
            IsMoveCommand = isMove;
            CreatedDate = created;
            ModifiedDate = modified;
            Description = desc;
        }
    }

    public class Map
    {
        // Primary key
        public int Id { get; set; }

        // Human-friendly map name
        public string Name { get; set; } = string.Empty;

        // Dimensions of the map
        public int Rows { get; set; }
        public int Columns { get; set; }

        // Timestamps for auditing
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        // Optional description
        public string? Description { get; set; }

        public Map() { }

        // Convenience constructor
        public Map(int id, string name, int rows, int cols, DateTime created, DateTime modified, string? desc)
        {
            Id = id;
            Name = name;
            Rows = rows;
            Columns = cols;
            CreatedDate = created;
            ModifiedDate = modified;
            Description = desc;
        }
    }
}

// Persistence namespace: interfaces, repository base, extension, and repository implementation.
// Note: RobotCommand and Map types are the root-namespace types above (robot_controller_api).
namespace robot_controller_api.Persistence
{
    // Interfaces used by controllers and DI
    // These define the contract for data access implementations (ADO or Repository).
    public interface IRobotCommandDataAccess
    {
        List<robot_controller_api.RobotCommand> GetAll();
        List<robot_controller_api.RobotCommand> GetMoveCommands();
        robot_controller_api.RobotCommand? GetById(int id);
        robot_controller_api.RobotCommand Insert(robot_controller_api.RobotCommand command);
        bool Update(int id, robot_controller_api.RobotCommand command);
        bool Delete(int id);
        bool NameExists(string name, int? excludeId = null);
    }

    public interface IMapDataAccess
    {
        List<robot_controller_api.Map> GetAll();
        List<robot_controller_api.Map> GetSquareMaps();
        robot_controller_api.Map? GetById(int id);
        robot_controller_api.Map Insert(robot_controller_api.Map map);
        bool Update(int id, robot_controller_api.Map map);
        bool Delete(int id);
        bool IsCoordinateOnMap(int id, int x, int y);
    }

    // RepositoryBase with generic ExecuteReader using FastMember mapping
    public abstract class RepositoryBase
    {
        // Connection string is read from environment by default.
        protected readonly string CONNECTION_STRING =
            Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Username=postgres;Password=;Database=sit331";

        // Generic helper that executes a SQL command and maps each row to T using MapTo extension.
        protected List<T> ExecuteReader<T>(string sqlCommand, NpgsqlParameter[]? dbParams = null) where T : class, new()
        {
            var entities = new List<T>();

            // Create and open a new connection for each operation (safe for DI-scoped repos).
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(sqlCommand, conn);

            // Add parameters only if provided and non-null values exist.
            if (dbParams is not null && dbParams.Length > 0)
            {
                cmd.Parameters.AddRange(dbParams.Where(x => x.Value is not null).ToArray());
            }

            // Execute reader and map each row to a new instance of T.
            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                var entity = new T();
                dr.MapTo(entity); // Extension method does the FastMember mapping
                entities.Add(entity);
            }

            return entities;
        }
    }

    // Extension method MapTo<T> using FastMember
    public static class ExtensionMethods
    {
        public static void MapTo<T>(this NpgsqlDataReader dr, T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            // Create a fast accessor for the entity type and collect property names (case-insensitive).
            var fastMember = TypeAccessor.Create(entity.GetType());
            var props = fastMember.GetMembers().Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Iterate columns and assign values to matching properties when present.
            for (int i = 0; i < dr.FieldCount; i++)
            {
                var columnName = dr.GetName(i);
                var prop = props.FirstOrDefault(x => x.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(prop))
                {
                    // Convert DB NULL to C# null so nullable properties can accept it.
                    var value = dr.IsDBNull(i) ? null : dr.GetValue(i);
                    fastMember[entity, prop] = value;
                }
            }
        }
    }

    // RobotCommandRepository implementation
    // Concrete repository that implements IRobotCommandDataAccess using RepositoryBase helpers.
    public class RobotCommandRepository : RepositoryBase, IRobotCommandDataAccess
    {
        // Return all robot commands ordered by id
        public List<robot_controller_api.RobotCommand> GetAll()
        {
            return ExecuteReader<robot_controller_api.RobotCommand>(
                "SELECT id, \"Name\", description, ismovecommand, createddate, modifieddate FROM public.robotcommand ORDER BY id");
        }

        // Return only commands that are movement commands
        public List<robot_controller_api.RobotCommand> GetMoveCommands()
        {
            return ExecuteReader<robot_controller_api.RobotCommand>(
                "SELECT id, \"Name\", description, ismovecommand, createddate, modifieddate FROM public.robotcommand WHERE ismovecommand = true ORDER BY id");
        }

        // Get a single command by id (uses parameterized query to avoid SQL injection)
        public robot_controller_api.RobotCommand? GetById(int id)
        {
            var list = ExecuteReader<robot_controller_api.RobotCommand>(
                "SELECT id, \"Name\", description, ismovecommand, createddate, modifieddate FROM public.robotcommand WHERE id = @id",
                new NpgsqlParameter[] { new("id", id) });
            return list.SingleOrDefault();
        }

        // Insert a new command and return the created entity using RETURNING
        public robot_controller_api.RobotCommand Insert(robot_controller_api.RobotCommand command)
        {
            var sql = @"INSERT INTO public.robotcommand (""Name"", description, ismovecommand, createddate, modifieddate)
                        VALUES (@name, @description, @ismove, @createddate, @modifieddate)
                        RETURNING id, ""Name"", description, ismovecommand, createddate, modifieddate";
            var sqlParams = new NpgsqlParameter[]
            {
                new("name", command.Name),
                // Use DBNull.Value when Description is null so DB receives proper NULL
                new("description", (object)command.Description ?? DBNull.Value),
                new("ismove", command.IsMoveCommand),
                new("createddate", command.CreatedDate),
                new("modifieddate", command.ModifiedDate)
            };
            var created = ExecuteReader<robot_controller_api.RobotCommand>(sql, sqlParams).Single();
            return created;
        }

        // Update an existing command; returns true when a row was affected
        public bool Update(int id, robot_controller_api.RobotCommand command)
        {
            var sql = @"UPDATE public.robotcommand
                        SET ""Name"" = @name,
                            description = @description,
                            ismovecommand = @ismove,
                            modifieddate = @modifieddate
                        WHERE id = @id";
            var sqlParams = new NpgsqlParameter[]
            {
                new("name", command.Name),
                new("description", (object)command.Description ?? DBNull.Value),
                new("ismove", command.IsMoveCommand),
                new("modifieddate", command.ModifiedDate),
                new("id", id)
            };

            // Use a direct ADO command for non-query update operations
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddRange(sqlParams.Where(p => p.Value is not null).ToArray());
            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        // Delete a command by id
        public bool Delete(int id)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM public.robotcommand WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        // Check whether a command name exists (optionally excluding a specific id)
        public bool NameExists(string name, int? excludeId = null)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();
            string sql = excludeId.HasValue
                ? "SELECT COUNT(1) FROM public.robotcommand WHERE lower(\"Name\") = lower(@name) AND id <> @id"
                : "SELECT COUNT(1) FROM public.robotcommand WHERE lower(\"Name\") = lower(@name)";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("name", name);
            if (excludeId.HasValue) cmd.Parameters.AddWithValue("id", excludeId.Value);
            var countObj = cmd.ExecuteScalar();
            var count = (countObj == null || countObj == DBNull.Value) ? 0 : Convert.ToInt32(countObj);
            return count > 0;
        }
    }
}

