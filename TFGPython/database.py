from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
import os
from schemas import Base


current_dir = os.path.dirname(os.path.abspath(__file__))


db_folder = os.path.join(current_dir, "BasesDeDatos")

if not os.path.exists(db_folder):
    os.makedirs(db_folder)
    print(f"LOG: Carpeta de base de datos creada en: {db_folder}")

DB_NAME = "TFG_DataBase.db"
DB_PATH = os.path.normpath(os.path.join(db_folder, DB_NAME))

DATABASE_URL = f"sqlite:///{DB_PATH}"

engine = create_engine(DATABASE_URL, connect_args={"check_same_thread": False})
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

def init_db():
    # Crea las tablas si no existen (equivalente al Awake de Unity)
    Base.metadata.create_all(bind=engine)

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()