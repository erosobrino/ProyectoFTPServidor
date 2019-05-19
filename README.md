# Proyecto Servidor FTP
El proyecto consiste en un servidor FTP al cual nos podremos conectar mediante una aplicación para Windows y Android, estas estarán desarrolladas en Xamarin y estará pronto disponibles.
Enlace del cliente: https://github.com/erosobrino/ClienteFTP


### Instalación 

Para poder utilizar este programa primero deberás tener acceso a una base de datos MYSQL y después deberás compilar el codigo de este repositorio.
Una vez hecho esto, puedes añadir nuevos usuarios as servidor mediante un gestor de base de datos como HeidiSQL.


## Realizado con 

* [Visual Studio 2017](https://visualstudio.microsoft.com/es/downloads/) - IDE
* [Conector MySQL/NET](https://dev.mysql.com/downloads/connector/net/) - Conector para hacer uso de bases de datos MySQL desde C#
* [XAMPP](https://www.apachefriends.org/es/index.html) - Sistema gestor de bases de datos.


## Avances

12/04/2019 Crear el proyecto<br>
19/04/2019 Metodos para leer configuracion, conectar bd o crear para tabla de usuarios, comprobacion de usuario valido y carga de ficheros<br>
23/04/2019 Metodo para lanzar hilos para conexiones, añadir nuevo elemento al fichero de configuracion y correccion en conexion a bd<br>
27/04/2019 Funcion hiloCLiente, comprueba usuario, muestra ficheros de la ruta de ese cliente, permite cambiar de directorio y salir<br>
01/05/2019 Crear proyecto Xamarin para el cliente y añadir pantalla principal en ClienteFTP<br>
04/05/2019 Pantalla de inicio, pantalla de listado con botones y visualizacion de listado con imagenes en ClienteFTP<br>
05/05/2019 Modificar layout de pagina inicio por grid, añadir permiso red android y paso de pantalla al conectar, añadir preferencias para guardar datos de inicio y pasar de campo con enter<br>
11/05/2019 Visualizar ficheros y carpetas en la lista, cambiar de carpeta, menu opciones iniciado, funcion para crear ficheros en windows y android(sin acabar) <br>
12/05/2019 Añadir Opcion apagar server y para cambiar contraseña (falta metodo) Server<br>
12/05/2019 Añadir pantalla de configuracion de usuario para cambiar contraseña, pagina de informacion(sin finalizar) y opcion de apagar server si somos admin. Cliente<br>
17/05/2019 Modificacion tabla, funcion de comprobar admin y cambio de contraseña de un usuario<br>
18/05/2019 Envio de ficheros en android funcional y pequeños cambios de diseño<br>
19/05/2019 Comprobaciones espacio, poder reconectarse, notificaciones, icono, pruebas archivos, funciona en android pero en windows no<br>
20/05/2019 Probado el cliente desde otro equipo funciona el envio de archivos
