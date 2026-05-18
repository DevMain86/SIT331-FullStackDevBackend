using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace robot_controller_api.Persistence
{
    // Repository for Map entities.
    // Implements IMapDataAccess and uses RepositoryBase helpers (ExecuteReader) for SELECTs.
    public class MapRepository : RepositoryBase, IMapDataAccess
    {
        // Return all maps ordered by id.
        // Uses the generic ExecuteReader<T> to map rows to Map objects.
        public List<Map> GetAll()
        {
            return ExecuteReader<Map>("SELECT id, name, description, rows, columns, issquare, createddate, modifieddate FROM public.map ORDER BY id");
        }

        // Return only maps flagged as square (issquare = true).
        public List<Map> GetSquareMaps()
        {
            return ExecuteReader<Map>("SELECT id, name, description, rows, columns, issquare, createddate, modifieddate FROM public.map WHERE issquare = true ORDER BY id");
        }

        // Get a single map by id. Uses a parameterized query to avoid SQL injection.
        // ExecuteReader returns a list; SingleOrDefault returns the single item or null if not found.
        public Map? GetById(int id)
        {
            var list = ExecuteReader<Map>("SELECT id, name, description, rows, columns, issquare, createddate, modifieddate FROM public.map WHERE id = @id",
                new NpgsqlParameter[] { new("id", id) });
            return list.SingleOrDefault();
        }

        // Insert a new map and return the created entity.
        // Uses RETURNING to fetch the inserted row in one round trip.
        public Map Insert(Map map)
        {
            var sql = @"INSERT INTO public.map (name, description, rows, columns, createddate, modifieddate)
                        VALUES (@name, @description, @rows, @columns, @createddate, @modifieddate)
                        RETURNING id, name, description, rows, columns, issquare, createddate, modifieddate";
            var sqlParams = new NpgsqlParameter[]
            {
                new("name", map.Name),
                // If Description is null, pass DBNull.Value so the DB receives NULL.
                new("description", (object)map.Description ?? DBNull.Value),
                new("rows", map.Rows),
                new("columns", map.Columns),
                new("createddate", map.CreatedDate),
                new("modifieddate", map.ModifiedDate)
            };
            // ExecuteReader maps the returned row to a Map instance; Single() assumes the INSERT returned one row.
            var created = ExecuteReader<Map>(sql, sqlParams).Single();
            return created;
        }

        // Update an existing map. Returns true if a row was affected.
        // Uses a parameterized UPDATE and ExecuteNonQuery for performance (no mapping needed).
        public bool Update(int id, Map map)
        {
            var sql = @"UPDATE public.map
                        SET name = @name,
                            description = @description,
                            rows = @rows,
                            columns = @columns,
                            modifieddate = @modifieddate
                        WHERE id = @id";
            var sqlParams = new NpgsqlParameter[]
            {
                new("name", map.Name),
                new("description", (object)map.Description ?? DBNull.Value),
                new("rows", map.Rows),
                new("columns", map.Columns),
                new("modifieddate", map.ModifiedDate),
                new("id", id)
            };

            // Use a fresh connection for the non-query operation.
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();
            using var cmd = new NpgsqlCommand(sql, conn);
            // Only add parameters with non-null values to avoid passing unintended nulls.
            cmd.Parameters.AddRange(sqlParams.Where(p => p.Value is not null).ToArray());
            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        // Delete a map by id. Returns true if a row was deleted.
        public bool Delete(int id)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM public.map WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);
            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        // Check whether a coordinate (x,y) lies within the bounds of the map with the given id.
        // Returns false for negative coordinates or if the map does not exist.
        public bool IsCoordinateOnMap(int id, int x, int y)
        {
            if (x < 0 || y < 0) return false;

            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT rows, columns FROM public.map WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            using var dr = cmd.ExecuteReader();
            // If the map id is not found, return false.
            if (!dr.Read()) return false;

            // Read rows and columns (assumed non-null in schema).
            var rows = dr.GetInt32(0);
            var columns = dr.GetInt32(1);

            // Coordinates are zero-based: x < columns and y < rows means the coordinate is on the map.
            return x < columns && y < rows;
        }
    }
}
