import pyodbc
import configparser

class ODBC:
    def __init__(self):
        config = configparser.ConfigParser()
        config.read("./config/config.ini")
        self.reference = config['reference']['path']

        self.server = config['database']['server']
        self.database = config['database']['database']
        self.username = config['database']['username']
        self.password = config['database']['password']

    def connect(self):
        self.cnxn = pyodbc.connect('DRIVER={ODBC Driver 13 for SQL Server};SERVER=' + self.server + ';DATABASE=' + self.database + ';UID=' + self.username + ';PWD=' + self.password +';Encrypt=yes;TrustServerCertificate=no;Connection Timeout=30;')
        self.cursor = self.cnxn.cursor()
    
    def getVersion(self):
        #Sample select query
        self.cursor.execute("SELECT @@version;") 
        row = self.cursor.fetchone() 
        while row: 
            print(row[0])
            row = self.cursor.fetchone()
    
    def getAll(self, table):
        self.cursor.execute('SELECT * FROM ' + self.database + '.dbo.' + table)
        for row in self.cursor:
            print(row)

    def insertValue(self, value):
        #Sample insert query
        self.cursor.execute("INSERT SalesLT.Product (Name, ProductNumber, StandardCost, ListPrice, SellStartDate) OUTPUT INSERTED.ProductID VALUES ('SQL Server Express New 20', 'SQLEXPRESS New 20', 0, 0, CURRENT_TIMESTAMP )") 
        self.cnxn.commit()
        row = self.cursor.fetchone()

        while row: 
            print 'Inserted Product key is ' + str(row[0]) 
            row = self.cursor.fetchone()