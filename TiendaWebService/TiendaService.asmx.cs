using System;
using System.Collections.Generic;
using System.Web.Services;
using MySql.Data.MySqlClient; 
using System.Configuration;   
using System.Data;

namespace TiendaWebService
{
    [WebService(Namespace = "http://tienda.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    public class TiendaService : System.Web.Services.WebService
    {
        readonly string connString = ConfigurationManager.ConnectionStrings["TiendaConn"].ConnectionString;

        // ==========================================
        // SECCIÓN 1: GESTIÓN DE USUARIOS 
        // ==========================================

        [WebMethod]
        public string RegistrarUsuario(string user, string pass, string nombre, string apellido, string email)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connString))
                {
                    // Validación de duplicados 
                    string checkQuery = "SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario=@u OR Email=@e";
                    MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                    checkCmd.Parameters.AddWithValue("@u", user);
                    checkCmd.Parameters.AddWithValue("@e", email);
                    conn.Open();
                    if (Convert.ToInt32(checkCmd.ExecuteScalar()) > 0) return "Error: El usuario o email ya existen.";

                    string query = "INSERT INTO Usuarios (NombreUsuario, Contraseña, Nombre, Apellido, Email, FechaRegistro) VALUES (@u, @p, @n, @a, @e, NOW())";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@u", user);
                    cmd.Parameters.AddWithValue("@p", pass);
                    cmd.Parameters.AddWithValue("@n", nombre);
                    cmd.Parameters.AddWithValue("@a", apellido);
                    cmd.Parameters.AddWithValue("@e", email);
                    cmd.ExecuteNonQuery();
                    return "Usuario registrado con éxito.";
                }
            }
            catch (Exception ex) { return "Error: " + ex.Message; }
        }

        [WebMethod]
        public bool ValidarUsuario(string user, string pass)
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = "SELECT COUNT(*) FROM Usuarios WHERE NombreUsuario=@u AND Contraseña=@p"; //
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@u", user);
                cmd.Parameters.AddWithValue("@p", pass);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        [WebMethod]
        public string ActualizarUsuario(int id, string nombre, string apellido, string email)
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = "UPDATE Usuarios SET Nombre=@n, Apellido=@a, Email=@e WHERE UsuarioID=@id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@a", apellido);
                cmd.Parameters.AddWithValue("@e", email);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0 ? "Datos actualizados." : "Usuario no encontrado.";
            }
        }

        [WebMethod]
        public string EliminarUsuario(int id)
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = "DELETE FROM Usuarios WHERE UsuarioID=@id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0 ? "Usuario eliminado." : "No se pudo eliminar.";
            }
        }

        [WebMethod]
        public DataTable ObtenerUsuarios()
        {
            DataTable dt = new DataTable("Usuarios");
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = "SELECT UsuarioID, NombreUsuario, Nombre, Apellido, Email FROM Usuarios";
                MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                da.Fill(dt);
            }
            return dt;
        }

        // ==========================================
        // SECCIÓN 2: GESTIÓN DE PRODUCTOS 
        // ==========================================

        [WebMethod]
        public string CrearProducto(string nombre, string desc, decimal precio, int stock, int catId)
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = "INSERT INTO Productos (Nombre, Descripción, Precio, Stock, CategoriaID) VALUES (@n, @d, @p, @s, @c)"; //
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@d", desc);
                cmd.Parameters.AddWithValue("@p", precio);
                cmd.Parameters.AddWithValue("@s", stock);
                cmd.Parameters.AddWithValue("@c", catId);
                conn.Open();
                cmd.ExecuteNonQuery();
                return "Producto creado.";
            }
        }

        [WebMethod]
        public string ActualizarProducto(int id, string nombre, decimal precio, int stock)
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = "UPDATE Productos SET Nombre=@n, Precio=@p, Stock=@s WHERE ProductoID=@id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@p", precio);
                cmd.Parameters.AddWithValue("@s", stock);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0 ? "Producto actualizado." : "ID no existe.";
            }
        }

        [WebMethod]
        public DataTable ObtenerProductos()
        {
            DataTable dt = new DataTable("Productos");
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = "SELECT * FROM Productos";
                MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                da.Fill(dt);
            }
            return dt;
        }

        [WebMethod]
        public DataTable BuscarProductos(string criterio)
        {
            DataTable dt = new DataTable("Resultados");
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = "SELECT * FROM Productos WHERE Nombre LIKE @c OR Descripción LIKE @c";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@c", "%" + criterio + "%");
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        // ==========================================
        // SECCIÓN 3: PEDIDOS Y LOGS 
        // ==========================================

        [WebMethod]
        public string CrearPedido(int usuarioId, List<DetalleLinea> productos)
        {
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                conn.Open();
                MySqlTransaction trans = conn.BeginTransaction(); // Transacciones 
                try
                {
                    string qPedido = "INSERT INTO Pedidos (UsuarioID, FechaPedido, Estado) VALUES (@u, NOW(), 'Pendiente'); SELECT LAST_INSERT_ID();";
                    MySqlCommand cmdP = new MySqlCommand(qPedido, conn, trans);
                    cmdP.Parameters.AddWithValue("@u", usuarioId);
                    int pedidoId = Convert.ToInt32(cmdP.ExecuteScalar());

                    foreach (var item in productos)
                    {
                        // Gestión de Inventario automática 
                        string qStk = "UPDATE Productos SET Stock = Stock - @c WHERE ProductoID = @prod AND Stock >= @c";
                        MySqlCommand cmdS = new MySqlCommand(qStk, conn, trans);
                        cmdS.Parameters.AddWithValue("@c", item.Cantidad);
                        cmdS.Parameters.AddWithValue("@prod", item.ProductoID);
                        if (cmdS.ExecuteNonQuery() == 0) throw new Exception("Stock insuficiente para el producto ID: " + item.ProductoID);

                        string qDet = "INSERT INTO DetallePedidos (PedidoID, ProductoID, Cantidad, PrecioUnitario) VALUES (@pid, @prod, @cant, @pre)";
                        MySqlCommand cmdD = new MySqlCommand(qDet, conn, trans);
                        cmdD.Parameters.AddWithValue("@pid", pedidoId);
                        cmdD.Parameters.AddWithValue("@prod", item.ProductoID);
                        cmdD.Parameters.AddWithValue("@cant", item.Cantidad);
                        cmdD.Parameters.AddWithValue("@pre", item.Precio);
                        cmdD.ExecuteNonQuery();
                    }
                    trans.Commit();
                    return "Pedido exitoso ID: " + pedidoId;
                }
                catch (Exception ex)
                {
                    trans.Rollback();
                    RegistrarLogError("CrearPedido", ex.Message); 
                    return "Error: " + ex.Message;
                }
            }
        }

        [WebMethod]
        public DataTable HistorialCompras(int usuarioId)
        {
            DataTable dt = new DataTable("Historial");
            using (MySqlConnection conn = new MySqlConnection(connString))
            {
                string query = @"SELECT p.PedidoID, p.FechaPedido, p.Estado, pr.Nombre, d.Cantidad 
                                 FROM Pedidos p 
                                 JOIN DetallePedidos d ON p.PedidoID = d.PedidoID 
                                 JOIN Productos pr ON d.ProductoID = pr.ProductoID 
                                 WHERE p.UsuarioID = @u";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@u", usuarioId);
                MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                da.Fill(dt);
            }
            return dt;
        }

        // Método de soporte para Logs 
        private void RegistrarLogError(string metodo, string error)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connString))
                {
                    string query = "INSERT INTO Logs (Metodo, Mensaje, Fecha) VALUES (@m, @e, NOW())";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@m", metodo);
                    cmd.Parameters.AddWithValue("@e", error);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* Silencioso para no interrumpir el flujo principal */ }
        }
    }

    public class DetalleLinea
    {
        public int ProductoID { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
    }
}