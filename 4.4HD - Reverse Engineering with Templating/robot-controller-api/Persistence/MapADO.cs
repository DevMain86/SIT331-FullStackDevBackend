// Persistence/MapADO.cs
using System;
using System.Collections.Generic;
using System.Data;
using Npgsql;
using robot_controller_api.Persistence.Helpers;

namespace robot_controller_api.Persistence
{
    // ADO.NET implementation of IMapDataAccess.
    public class MapADO : IMapDataAccess
    {
        private readonly string CONNECTION_STRING =
            Environment.GetEnvironmentVariable("DB_CONNECTION")
            ?? "Host=localhost;Username=postgres;Password=;Database=sit331";

        // Helper: get ordinal by column name, throw if not present
        private static int GetOrdinalSafe(IDataRecord dr, string columnName)
        {
            try
            {
                return dr.GetOrdinal(columnName);
            }
            catch (IndexOutOfRangeException)
            {
                throw new InvalidOperationException($"Expected column '{columnName}' not found in query result.");
            }
        }

        public List<Map> GetAll()
        {
            var result = new List<Map>();
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, name, description, rows, columns, issquare, createddate, modifieddate FROM public.map ORDER BY id",
                conn);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                // Resolve ordinals by name for robustness
                int ordId = GetOrdinalSafe(dr, "id");
                int ordName = GetOrdinalSafe(dr, "name");
                int ordDescription = GetOrdinalSafe(dr, "description");
                int ordRows = GetOrdinalSafe(dr, "rows");
                int ordColumns = GetOrdinalSafe(dr, "columns");
                int ordCreated = GetOrdinalSafe(dr, "createddate");
                int ordModified = GetOrdinalSafe(dr, "modifieddate");

                string? descr = dr.IsDBNull(ordDescription) ? null : dr.GetString(ordDescription);

                var map = new Map(
                    dr.GetInt32(ordId),
                    dr.GetString(ordName),
                    dr.GetInt32(ordRows),
                    dr.GetInt32(ordColumns),
                    dr.GetDateTime(ordCreated),
                    dr.GetDateTime(ordModified),
                    descr
                );

                result.Add(map);
            }

            return result;
        }

        public List<Map> GetSquareMaps()
        {
            var result = new List<Map>();
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, name, description, rows, columns, issquare, createddate, modifieddate FROM public.map WHERE issquare = true ORDER BY id",
                conn);

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                int ordId = GetOrdinalSafe(dr, "id");
                int ordName = GetOrdinalSafe(dr, "name");
                int ordDescription = GetOrdinalSafe(dr, "description");
                int ordRows = GetOrdinalSafe(dr, "rows");
                int ordColumns = GetOrdinalSafe(dr, "columns");
                int ordCreated = GetOrdinalSafe(dr, "createddate");
                int ordModified = GetOrdinalSafe(dr, "modifieddate");

                string? descr = dr.IsDBNull(ordDescription) ? null : dr.GetString(ordDescription);

                var map = new Map(
                    dr.GetInt32(ordId),
                    dr.GetString(ordName),
                    dr.GetInt32(ordRows),
                    dr.GetInt32(ordColumns),
                    dr.GetDateTime(ordCreated),
                    dr.GetDateTime(ordModified),
                    descr
                );

                result.Add(map);
            }

            return result;
        }

        public Map? GetById(int id)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, name, description, rows, columns, issquare, createddate, modifieddate FROM public.map WHERE id = @id",
                conn);
            cmd.Parameters.AddWithValue("id", id);

            using var dr = cmd.ExecuteReader();
            if (!dr.Read()) return null;

            int ordId = GetOrdinalSafe(dr, "id");
            int ordName = GetOrdinalSafe(dr, "name");
            int ordDescription = GetOrdinalSafe(dr, "description");
            int ordRows = GetOrdinalSafe(dr, "rows");
            int ordColumns = GetOrdinalSafe(dr, "columns");
            int ordCreated = GetOrdinalSafe(dr, "createddate");
            int ordModified = GetOrdinalSafe(dr, "modifieddate");

            string? descr = dr.IsDBNull(ordDescription) ? null : dr.GetString(ordDescription);

            return new Map(
                dr.GetInt32(ordId),
                dr.GetString(ordName),
                dr.GetInt32(ordRows),
                dr.GetInt32(ordColumns),
                dr.GetDateTime(ordCreated),
                dr.GetDateTime(ordModified),
                descr
            );
        }

        public Map Insert(Map map)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO public.map (name, description, rows, columns, createddate, modifieddate)
                  VALUES (@name, @description, @rows, @columns, @createddate, @modifieddate)
                  RETURNING id",
                conn);

            // Bind parameters using snake_case names (DB expects snake_case)
            cmd.Parameters.AddWithValue("name", map.Name);
            cmd.Parameters.AddWithValue("description", (object)map.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("rows", map.Rows);
            cmd.Parameters.AddWithValue("columns", map.Columns);
            cmd.Parameters.AddWithValue("createddate", map.CreatedDate);
            cmd.Parameters.AddWithValue("modifieddate", map.ModifiedDate);

            var idObj = cmd.ExecuteScalar();
            if (idObj == null || idObj == DBNull.Value)
                throw new InvalidOperationException("Failed to insert map: no id returned.");

            var id = Convert.ToInt32(idObj);
            return new Map(id, map.Name, map.Rows, map.Columns, map.CreatedDate, map.ModifiedDate, map.Description);
        }

        public bool Update(int id, Map map)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                @"UPDATE public.map
                  SET name = @name,
                      description = @description,
                      rows = @rows,
                      columns = @columns,
                      modifieddate = @modifieddate
                  WHERE id = @id",
                conn);

            cmd.Parameters.AddWithValue("name", map.Name);
            cmd.Parameters.AddWithValue("description", (object)map.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("rows", map.Rows);
            cmd.Parameters.AddWithValue("columns", map.Columns);
            cmd.Parameters.AddWithValue("modifieddate", map.ModifiedDate);
            cmd.Parameters.AddWithValue("id", id);

            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        public bool Delete(int id)
        {
            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM public.map WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            var affected = cmd.ExecuteNonQuery();
            return affected > 0;
        }

        public bool IsCoordinateOnMap(int id, int x, int y)
        {
            if (x < 0 || y < 0) return false;

            using var conn = new NpgsqlConnection(CONNECTION_STRING);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT rows, columns FROM public.map WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", id);

            using var dr = cmd.ExecuteReader();
            if (!dr.Read()) return false;

            int ordRows = GetOrdinalSafe(dr, "rows");
            int ordColumns = GetOrdinalSafe(dr, "columns");

            var rows = dr.GetInt32(ordRows);
            var columns = dr.GetInt32(ordColumns);

            return x < columns && y < rows;
        }
    }
}
