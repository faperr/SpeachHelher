
USE ProductionDB;
GO

INSERT INTO Customers
    (id, name, inn, addres, phone, type)
SELECT
    j.id,
    j.name,
    j.inn,
    j.addres,
    j.phone,
    j.type
FROM OPENROWSET(
    BULK 'C:\Users\gvyu3\OneDrive\Рабочий стол\Ddsdf\Заказчики_1.json',
    SINGLE_CLOB,
    CODEPAGE = '1251'
) AS f
CROSS APPLY OPENJSON(f.BulkColumn)
WITH (
    id NVARCHAR(20) '$.id',
    name NVARCHAR(255) '$.name',
    inn NVARCHAR(20) '$.inn',
    addres NVARCHAR(500) '$.addres',
    phone NVARCHAR(50) '$.phone',
    type NVARCHAR(50) '$.type'
) AS j
WHERE NOT EXISTS (
    SELECT 1
    FROM Customers c
    WHERE c.id = j.id
);
GO

SELECT * FROM Customers;




USE ProductionDB;
GO

SELECT
    o.id AS OrderID,
    c.name AS CustomerName,
    CAST(
        SUM(oi.quantity * s.qty * pr.price)
        AS DECIMAL(18,2)
    ) AS TotalCost
FROM Orders o
JOIN Customers c
    ON o.customer_id = c.id
JOIN Order_items oi
    ON o.id = oi.order_id
JOIN Specification s
    ON oi.product_id = s.product_id
JOIN Materials m
    ON s.material_id = m.id
JOIN Prices pr
    ON m.nomenclature_id = pr.item_id
GROUP BY
    o.id,
    c.name
ORDER BY
    o.id;








using System.Data;
using System.Data.SqlClient;

namespace Demo
{
    internal class DataBase
    {
        private SqlConnection sqlConnection = new SqlConnection(
            @"Data Source=MSI;Initial Catalog=ProductionDB;Integrated Security=True"
        );

        public void OpenConnection()
        {
            if (sqlConnection.State == ConnectionState.Closed)
                sqlConnection.Open();
        }

        public void CloseConnection()
        {
            if (sqlConnection.State == ConnectionState.Open)
                sqlConnection.Close();
        }

        public SqlConnection GetConnection()
        {
            return sqlConnection;
        }
    }
}













using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Demo
{
    public partial class Form1 : Form
    {
        DataBase dataBase = new DataBase();

        // Количество неправильных попыток
        private int failedAttempts = 0;

        // Максимальное количество попыток
        private const int MaxAttempts = 5;

        public Form1()
        {
            InitializeComponent();
             
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string login = textBox1.Text.Trim();
            string password = textBox2.Text;

            // Проверка пустых полей
            if (string.IsNullOrWhiteSpace(login) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Поля \"Логин\" и \"Пароль\" обязательны для заполнения.",
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                return;
            }

            try
            {
                dataBase.OpenConnection();

                string query =
                    @"SELECT Роль
                      FROM Пользователи
                      WHERE Логин = @login
                      AND Пароль = @password";

                SqlCommand command =
                    new SqlCommand(query, dataBase.GetConnection());
                                                                                                                                                            
                command.Parameters.Add(
                    "@login",
                    SqlDbType.NVarChar,
                    50
                ).Value = login;

                command.Parameters.Add(
                    "@password",
                    SqlDbType.NVarChar,
                    100
                ).Value = password;

                object result = command.ExecuteScalar();

                // Если логин и пароль правильные
                if (result != null)
                {
                    failedAttempts = 0;

                    string role = result.ToString();

                    MessageBox.Show(
                        "Вы успешно авторизовались",
                        "Авторизация",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    if (role == "Администратор")
                    {
                        AdminPanel adminPanel = new AdminPanel();
                        adminPanel.Show();

                        this.Hide();
                    }
                    else if (role == "Пользователь")
                    {
                        MessageBox.Show(
                            "Вы вошли как пользователь.",
                            "Пользователь",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }
                else
                {
                    // Увеличиваем количество неправильных попыток
                    failedAttempts++;

                    // Если попыток 5 или больше
                    if (failedAttempts >= MaxAttempts)
                    {
                        MessageBox.Show(
                            "Вы сделали слишком много неудачных попыток. Вы заблокированы.",
                            "Блокировка",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );

                        // Блокируем поля и кнопку
                        textBox1.Enabled = false;
                        textBox2.Enabled = false;
                        button1.Enabled = false;

                        return;
                    }

                    int remainingAttempts = MaxAttempts - failedAttempts;

                    MessageBox.Show(
                        "Вы ввели неверный логин или пароль. " +
                        "Пожалуйста проверьте ещё раз введенные данные.\n\n" +
                        "Осталось попыток: " + remainingAttempts,
                        "Ошибка авторизации",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка подключения к базе данных:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                dataBase.CloseConnection();
            }
        }
    }
}



