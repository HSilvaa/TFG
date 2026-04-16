from sqlalchemy import create_engine
from sqlalchemy.orm import sessionmaker
import os
from schemas import Base

# Ajusta esta ruta a la que usa tu Unity
DB_PATH = "C:/Users/hugop/Desktop/TFG/ProyectoUnity/AIGERIM-TFG/BasesDeDatos/TFG_DataBase.db"
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