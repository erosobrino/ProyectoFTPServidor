using System;
using MySql.Data.MySqlClient;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

namespace ProyectoFTPServidor
{
    class Program
    {
        IPAddress ip;
        string puertoBD;
        int puertoServer;
        int puertoArchivos;
        string usuarioBD;
        string contraseñaBD;
        string nombreBDUsuarios;
        string ruta;
        string rutaConfig = Environment.GetEnvironmentVariable("homedrive") + "\\" + Environment.GetEnvironmentVariable("homepath") + "\\configServidorFTP.txt";
        List<string> rutasFicheros = new List<string>();
        List<string> rutasDirectorios = new List<string>();

        /// <summary>
        /// Comprueba la configuración y la crea en caso de que no exista,
        /// se conecta a la base de datos y lanza el inicio de los hilos.
        /// 
        /// Por pantalla se  muestra informacion del servidor.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Program p = new Program();
            p.leeConfiguracion();

            Console.WriteLine("Puedes modificar el archivo de configuracion en " + p.rutaConfig);
            Console.Write("IP: " + p.ip + " ");
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {

                    Console.Write(ip.ToString() + " ");
                }
            }
            Console.WriteLine();
            Console.WriteLine("Usuario: " + p.usuarioBD);
            Console.WriteLine("Contraseña: " + p.contraseñaBD);
            Console.WriteLine("Base de datos: " + p.nombreBDUsuarios);
            Console.WriteLine("Puerto BD: " + p.puertoBD);
            Console.WriteLine("Puerto Servidor: " + p.puertoServer);
            Console.WriteLine("Puerto Archivos: " + p.puertoArchivos);
            Console.WriteLine("Ruta: " + p.ruta + "\n");

            if (p.puertoArchivos != Convert.ToInt32(p.puertoBD) && p.puertoServer != Convert.ToInt32(p.puertoBD) && p.puertoArchivos != p.puertoServer)
            {
                p.cargaFicheros();
                if (!p.compruebaBD())
                {
                    Console.WriteLine("No se ha podido establecer conexion con la base de datos\n");
                    Console.ReadLine();
                }
                else
                {
                    Console.WriteLine("Conexion correcta con la base de datos\n");
                    p.iniciaServidorArchivos();
                }
            }
            else
                Console.WriteLine("La configuracion no es valida, los puertos deben ser diferentes");
        }

        /// <summary>
        /// Intenta crear una entrada por red a traves del puerto de configuracion y por cada conexion crea un hilo
        /// </summary>
        public void iniciaServidorArchivos()
        {
            try
            {
                IPEndPoint ie = new IPEndPoint(IPAddress.Any, puertoServer);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                s.Bind(ie);
                s.Listen(10);
                Console.WriteLine("Escuchando en el puerto " + puertoServer);
                while (true)
                {
                    Socket sCliente = s.Accept();
                    Thread hilo = new Thread(() => hiloCliente(sCliente));
                    hilo.IsBackground = true;
                    hilo.Start();
                }
            }
            catch (SocketException)
            {
                Console.WriteLine("El puerto esta ocupado");
            }
        }

        /// <summary>
        /// En esta funcion se establece el protocolo de comunicación con el cliente, el cual dispondrá de distintas opciones.
        /// </summary>
        /// <param name="sCliente">El socket del cliente que se conecta</param>
        private void hiloCliente(Socket sCliente)
        {
            string ipCliente = ((IPEndPoint)(sCliente.RemoteEndPoint)).Address.ToString();
            NetworkStream ns;
            StreamReader sr;
            StreamWriter sw = null;
            bool stop = false;
            bool valido = false;
            string msg = "";
            string rutaActual = ruta;
            try
            {
                using (ns = new NetworkStream(sCliente))
                {
                    using (sr = new StreamReader(ns))
                    {
                        using (sw = new StreamWriter(ns))
                        {
                            sw.WriteLine("Conectado");
                            sw.Flush();
                            string userPass = sr.ReadLine(); //user=admin pass=admin
                            if (userPass != null && userPass != "")
                            {
                                userPass = userPass.Trim();
                                int longitud = userPass.IndexOf(' ') - userPass.IndexOf('=') - 1;
                                if (longitud > 0)
                                {
                                    string usuario = userPass.Substring(userPass.IndexOf('=') + 1, longitud);
                                    string contraseña = userPass.Substring(userPass.LastIndexOf('=') + 1);
                                    valido = usuarioValido(usuario, contraseña);
                                    if (!valido)
                                    {
                                        sw.WriteLine("invalido");
                                        sw.Flush();
                                    }
                                    else
                                    {
                                        sw.WriteLine("valido");
                                        sw.Flush();
                                        while (msg != null && !stop)
                                        {
                                            msg = sr.ReadLine();
                                            if (msg != null)
                                            {
                                                string[] msgSeparado = msg.Split(' ');
                                                switch (msgSeparado[0].ToUpper())
                                                {
                                                    case "LISTADO":
                                                        sw.WriteLine(carpetaActual(rutaActual));
                                                        sw.Flush();
                                                        break;
                                                    case "DIRECTORIO":
                                                        if (rutasDirectorios.Contains((rutaActual + "\\" + msgSeparado[1].Trim()).ToLower()))
                                                        {
                                                            rutaActual += "\\" + msgSeparado[1].Trim();
                                                            sw.WriteLine("Valido");
                                                        }
                                                        else
                                                            sw.WriteLine("Invalido");
                                                        sw.Flush();
                                                        break;
                                                    case "FICHERO":
                                                        if (rutasFicheros.Contains((rutaActual + "\\" + msgSeparado[1].Trim()).ToLower()))
                                                        {
                                                            try
                                                            {
                                                                long ti = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();

                                                                using (Socket sArchivo = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                                                                {
                                                                    sArchivo.Connect(new IPEndPoint(IPAddress.Parse(ipCliente), puertoArchivos));
                                                                    sArchivo.SendFile((rutaActual + "\\" + msgSeparado[1].Trim()).ToLower());
                                                                    sArchivo.Close();
                                                                }

                                                                //sw.Write("enviado");
                                                                Console.WriteLine(System.DateTimeOffset.Now.ToUnixTimeMilliseconds() - ti);
                                                                //195829milis 3.2 min 546mb
                                                            }
                                                            catch
                                                            {
                                                                Console.WriteLine("Error al enviar un fichero");
                                                                //sw.WriteLine("Error al enviar el fichero");
                                                            }
                                                        }
                                                        else
                                                        {
                                                            //sw.WriteLine("No existe el fichero");
                                                        }
                                                        //sw.Flush();
                                                        break;
                                                    case "SALIR":
                                                        stop = true;
                                                        break;
                                                    case "CERRAR":
                                                        Environment.Exit(1);
                                                        break;
                                                    case "ATRAS":
                                                        if (rutaActual != ruta)
                                                        {
                                                            rutaActual = rutaActual.Substring(0, rutaActual.LastIndexOf("\\"));
                                                        }
                                                        else
                                                        {
                                                            sw.WriteLine("Ya estas en la raiz");
                                                            sw.Flush();
                                                        }
                                                        break;
                                                    case "USUARIO":
                                                        if (msgSeparado.Length == 3)
                                                        {
                                                            bool modificado = cambiaContraseñaBD(msgSeparado[1], msgSeparado[2]);
                                                            if (modificado)
                                                            {
                                                                sw.WriteLine("valido");
                                                            }
                                                            else
                                                                sw.WriteLine("invalido");
                                                            sw.Flush();
                                                        }
                                                        break;
                                                    case "ADMIN":
                                                        if (msgSeparado.Length == 2)
                                                        {
                                                            bool esAdmin = compruebaAdmin(msgSeparado[1]);
                                                            if (esAdmin)
                                                            {
                                                                sw.WriteLine("valido");
                                                            }
                                                            else
                                                                sw.WriteLine("invalido");
                                                            sw.Flush();
                                                        }
                                                        break;
                                                    case "LISTAUSUARIOS":
                                                        sw.WriteLine(listaUsuarios());
                                                        sw.Flush();
                                                        break;
                                                    case "MODIFICAR":
                                                        if (msgSeparado.Length == 5)
                                                        {
                                                            try
                                                            {
                                                                bool admin = false;
                                                                Boolean.TryParse(msgSeparado[4], out admin);
                                                                if (creaOModifica(Convert.ToInt32(msgSeparado[1]), msgSeparado[2], msgSeparado[3], admin))
                                                                    sw.WriteLine("valido");
                                                                else
                                                                    sw.WriteLine("error");
                                                            }
                                                            catch
                                                            {
                                                                sw.WriteLine("error");
                                                            }
                                                        }
                                                        else
                                                            sw.WriteLine("invalido");
                                                        sw.Flush();
                                                        break;
                                                    case "ELIMINAR":
                                                        if (msgSeparado.Length == 2)
                                                        {
                                                            try
                                                            {
                                                                if (eliminaUsuario(Convert.ToInt32(msgSeparado[1])))
                                                                    sw.WriteLine("valido");
                                                                else
                                                                    sw.WriteLine("error");
                                                            }
                                                            catch
                                                            {
                                                                sw.WriteLine("error");
                                                            }
                                                        }
                                                        else
                                                            sw.WriteLine("invalido");
                                                        sw.Flush();
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    sw.WriteLine("invalido");
                                    sw.Flush();
                                }
                            }
                        }
                    }
                    sCliente.Close();
                }
            }
            catch (IOException)
            {
                Console.WriteLine("Usuario desconectado");
                sCliente.Close();
            }
        }

        /// <summary>
        /// Comprueba si existe un usuario y lo elimina
        /// </summary>
        /// <param name="id">El id a eliminar</param>
        /// <returns>True si se elimina, false si no</returns>
        private bool eliminaUsuario(int id)
        {
            try
            {
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string comprobacion = "select * from usuarios where id=" + id;
                string elimina = "DELETE FROM usuarios WHERE id=" + id;

                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(comprobacion, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                    {
                        commandDatabase = new MySqlCommand(elimina, conBDUsuarios);
                        commandDatabase.ExecuteNonQuery();
                        return true;
                    }
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Inserta o añade a un usuario y devuelve true o false si se ha realizado
        /// la accion correctamente, si el id es -1 se inserta
        /// </summary>
        /// <param name="id">El id</param>
        /// <param name="nombre">El nombre de usuario</param>
        /// <param name="contraseña">La contraseña del usuario</param>
        /// <param name="admin">Si es admin</param>
        /// <returns>True si se modifica, false si no</returns>
        private bool creaOModifica(int id, string nombre, string contraseña, bool admin)
        {
            try
            {
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string insertar = "insert into usuarios(nombre, contraseña, esAdmin) VALUES('" + nombre + "', '" + contraseña + "', " + admin + ")";
                string modificar = "UPDATE usuarios SET nombre = '" + nombre + "', contraseña = '" + contraseña + "', esAdmin = " + admin + " WHERE id = " + id;
                string comprobacion = "select * from usuarios where nombre='" + nombre + "' and contraseña='" + contraseña + "' and esAdmin=" + admin;

                string query = "";
                if (id == -1)
                    query = insertar;
                else
                    query = modificar;

                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(query, conBDUsuarios);
                    commandDatabase.ExecuteNonQuery();
                    commandDatabase = new MySqlCommand(comprobacion, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                        return true;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Se conecta a la base de datos y devuelve los datos de los usuarios (id, nombre, contraseña,esAdmin)
        /// </summary>
        /// <returns>Los datos separados por | y cada usuario por #</returns>
        private string listaUsuarios()
        {
            string cadena = "";
            try
            {
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string query = "select * from usuarios";
                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(query, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                        while (reader.Read())
                        {
                            cadena += reader.GetInt32("id") + "|" + reader.GetString("nombre") + '|' + reader.GetString(2) + '|' + reader.GetBoolean("esAdmin") + '#';
                        }
                }
                return cadena;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Comrpueba si un usuario es admin
        /// </summary>
        /// <param name="nombre">El nombre a comprobar</param>
        /// <returns>True si es admin, false si no lo es</returns>
        private bool compruebaAdmin(string nombre)
        {
            try
            {
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string queryComprueba = "select * from usuarios where nombre='" + nombre + "' and esAdmin=true";
                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(queryComprueba, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                        return true;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cambia la conraseña de un usuario
        /// </summary>
        /// <param name="nombre">El nombre de usuario al que se le cambia la contraseña</param>
        /// <param name="contraseña">La nueva contraseña</param>
        /// <returns>True si se cambia, false si no</returns>
        private bool cambiaContraseñaBD(string nombre, string contraseña)
        {
            try
            {
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string query = "UPDATE usuarios SET contraseña='" + contraseña + "' WHERE nombre='" + nombre + "'";
                string queryComprueba = "select contraseña from usuarios where nombre='" + nombre + "' and contraseña='" + contraseña + "'";
                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(query, conBDUsuarios);
                    commandDatabase.ExecuteNonQuery();
                    commandDatabase = new MySqlCommand(queryComprueba, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                        return true;
                    else
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deveulve los nombres y el tamaño de cada fichero  y el nombre de las carpeta D(directorio) F(fichero) separados por ?
        /// </summary>
        /// <param name="ruta"></param>
        /// <returns>Los nombres de los ficheros y carpetas con indicativos para diferenciarlos</returns>
        private string carpetaActual(string ruta)
        {
            String nombres = "";
            DirectoryInfo directoryInfo = new DirectoryInfo(ruta);
            DirectoryInfo[] directorios = directoryInfo.GetDirectories();
            foreach (DirectoryInfo directorio in directorios)
            {
                nombres += "D:" + directorio.Name + '?';
            }
            FileInfo[] fileInfos = directoryInfo.GetFiles();
            foreach (FileInfo fileInfo in fileInfos)
            {
                nombres += "F:" + fileInfo.Name + '#' + fileInfo.Length + '?';
            }
            return nombres;
        }

        /// <summary>
        /// Llama a la funcion compruebaDirectorios con el indice 0 si existe la ruta actual
        /// </summary>
        private void cargaFicheros()
        {
            if (Directory.Exists(ruta))
            {
                compruebaDirectorios(ruta, 0);
            }
            else
            {
                Console.WriteLine("Debes indicar una ruta en el fichero de configuracion " + rutaConfig);
            }
        }

        /// <summary>
        /// Recorre todos los ficheros y los añade a una lista de string, en caso de ser carpetas se llama de forma recursiva 
        /// </summary>
        /// <param name="ruta">La ruta a comprobar</param>
        /// <param name="indice">El indice de profundidad de la carpeta</param>
        private void compruebaDirectorios(string ruta, int indice)
        {
            try
            {
                indice++;
                DirectoryInfo directoryInfo = new DirectoryInfo(ruta);
                FileInfo[] fileInfos = directoryInfo.GetFiles();
                for (int i = 0; i < indice - 1; i++)
                {
                    Console.Write("\t");
                }
                Console.WriteLine(directoryInfo.Name);
                foreach (FileInfo fileInfo in fileInfos)
                {
                    for (int j = 0; j < indice; j++)
                    {
                        Console.Write("\t");
                    }
                    Console.WriteLine(fileInfo.Name);
                    rutasFicheros.Add(fileInfo.FullName.ToLower());
                }
                Console.WriteLine();
                DirectoryInfo[] directorios = directoryInfo.GetDirectories();
                foreach (DirectoryInfo directorio in directorios)
                {
                    rutasDirectorios.Add(directorio.FullName.ToLower());
                    compruebaDirectorios(directorio.FullName, indice);
                }
            }
            catch
            {
                Console.WriteLine("Ruta no valida");
            }
        }

        /// <summary>
        /// Lee la configuracion de rutaConfig y si no existe la crea con datos por defecto
        /// </summary>
        private void leeConfiguracion()
        {
            if (File.Exists(rutaConfig))
            {
                try
                {
                    using (StreamReader sr = new StreamReader(rutaConfig))
                    {
                        string lineaActual = sr.ReadLine();
                        while (lineaActual != null)
                        {
                            switch (lineaActual.Substring(0, lineaActual.IndexOf('=')).ToLower())
                            {
                                case "ip":
                                    ip = IPAddress.Parse(lineaActual.Substring(lineaActual.IndexOf('=') + 1));
                                    break;
                                case "usuariobd":
                                    usuarioBD = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "contraseñabd":
                                    contraseñaBD = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "nombrebdusuarios":
                                    nombreBDUsuarios = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "puertobd":
                                    puertoBD = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "ruta":
                                    ruta = lineaActual.Substring(lineaActual.IndexOf('=') + 1);
                                    break;
                                case "puertoserver":
                                    try
                                    {
                                        puertoServer = Convert.ToInt32(lineaActual.Substring(lineaActual.IndexOf('=') + 1));
                                        if (puertoServer < IPEndPoint.MinPort || puertoServer > IPEndPoint.MaxPort)
                                        {
                                            throw new Exception();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("El puerto para el servidor debe ser un numero entre " + IPEndPoint.MinPort + " y " + IPEndPoint.MaxPort);
                                    }
                                    break;
                                case "puertoarchivos":
                                    try
                                    {
                                        puertoArchivos = Convert.ToInt32(lineaActual.Substring(lineaActual.IndexOf('=') + 1));
                                        if (puertoArchivos < IPEndPoint.MinPort || puertoArchivos > IPEndPoint.MaxPort)
                                        {
                                            throw new Exception();
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        Console.WriteLine("El puerto de archivos debe ser un numero entre " + IPEndPoint.MinPort + " y " + IPEndPoint.MaxPort);
                                    }
                                    break;
                                default:
                                    break;
                            }
                            lineaActual = sr.ReadLine();
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Error leyendo el archivo de configuracion");
                }
            }
            else
            {
                try
                {
                    using (StreamWriter sw = File.CreateText(rutaConfig))
                    {
                        sw.WriteLine("ip=127.0.0.1");
                        sw.WriteLine("usuarioBD=root");
                        sw.WriteLine("contraseñaBD=");
                        sw.WriteLine("nombreBDUsuarios=UsuariosFTP");
                        sw.WriteLine("puertoBD=3306");
                        sw.WriteLine("ruta=");
                        sw.WriteLine("puertoServer=31416");
                        sw.WriteLine("puertoArchivos=31417");
                    }
                    leeConfiguracion();
                }
                catch (Exception)
                {
                    Console.WriteLine("Error creando el archivo de configuracion");
                }
            }
        }

        /// <summary>
        /// Comprueba si existe la base de datos y sino la crea con un usuario por defecto
        /// </summary>
        /// <returns></returns>
        private bool compruebaBD()
        {
            try
            {
                //Comprueba que existe la base de datos y sino la crea con el usuario admin por defecto
                string conexion = "Server=" + ip + ";port=" + puertoBD + ";Database=mysql;User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                string query = "SHOW DATABASES LIKE '" + nombreBDUsuarios + "'";

                using (MySqlConnection con = new MySqlConnection(conexion))
                {
                    MySqlCommand commandDatabase;
                    commandDatabase = new MySqlCommand(query, con);
                    MySqlDataReader reader;
                    con.Open();
                    reader = commandDatabase.ExecuteReader();
                    if (!reader.HasRows)
                    {
                        reader.Close();
                        Console.WriteLine("No hay bd");
                        string creaBD = "CREATE DATABASE " + nombreBDUsuarios + " CHARACTER SET UTF8 COLLATE UTF8_SPANISH_CI;";
                        string creaTabla = "CREATE TABLE usuarios (id INT AUTO_INCREMENT PRIMARY KEY," +
                            "nombre CHAR(20) NOT NULL unique," +
                            "contraseña varchar(50) NOT NULL," +
                            " esAdmin BOOL DEFAULT FALSE);";
                        string añadeAdmin = "insert into usuarios (nombre, contraseña, esAdmin) VALUES ('admin', 'admin',true);";

                        commandDatabase = new MySqlCommand(creaBD, con);
                        commandDatabase.ExecuteNonQuery();

                        string conexionBDUsuarios = "Server=" + ip + ";Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                        using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                        {
                            conBDUsuarios.Open();
                            commandDatabase = new MySqlCommand(creaTabla, conBDUsuarios);
                            commandDatabase.ExecuteNonQuery();
                            commandDatabase = new MySqlCommand(añadeAdmin, conBDUsuarios);
                            commandDatabase.ExecuteNonQuery();
                        }
                        Console.WriteLine("Se ha creado la base de datos " + nombreBDUsuarios + " para almacenar los usuarios");
                    }
                }
                return true;
            }
            catch (MySqlException)
            {
                return false;
            }
        }

        /// <summary>
        /// Comprueba si un usuario pertenece a la base de datos
        /// </summary>
        /// <param name="nombre">El nombre a comprobar</param>
        /// <param name="contraseña">La contraseña a comprobar</param>
        /// <returns>True si son validos, false si no lo son</returns>
        private bool usuarioValido(string nombre, string contraseña)
        {
            try
            {
                string query = "SELECT * FROM usuarios WHERE nombre = '" + nombre + "' AND contraseña = '" + contraseña + "'";
                string conexionBDUsuarios = "Server=" + ip + ";port=" + puertoBD + "; Database=" + nombreBDUsuarios + ";User ID=" + usuarioBD + ";Password=" + contraseñaBD + ";Pooling=false;";
                using (MySqlConnection conBDUsuarios = new MySqlConnection(conexionBDUsuarios))
                {
                    conBDUsuarios.Open();
                    MySqlCommand commandDatabase = new MySqlCommand(query, conBDUsuarios);
                    MySqlDataReader reader = commandDatabase.ExecuteReader();
                    if (reader.HasRows)
                        return true;
                    else
                        return false;
                }
            }
            catch (MySqlException)
            {
                Console.WriteLine("Error al comprobar usuario");
                return false;
            }
        }
    }
}
