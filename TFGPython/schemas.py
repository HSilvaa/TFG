from sqlalchemy import Column, Integer, String, DateTime, ForeignKey, Text
from sqlalchemy.orm import relationship
from sqlalchemy.ext.declarative import declarative_base
from datetime import datetime

Base = declarative_base()

class Character(Base):
    __tablename__ = 'Character'
    id = Column(Integer, primary_key=True, autoincrement=True)
    name = Column(String(100), nullable=False)
    age = Column(String(50))
    description = Column(Text)
    epoca = Column(String(100))
    created_at = Column(DateTime, default=datetime.utcnow)

    # Relaciones: permiten acceder a char.conversations o char.resumen
    conversations = relationship("Conversation", backref="character", cascade="all, delete-orphan")
    resumen = relationship("Resumen", backref="character", uselist=False, cascade="all, delete-orphan")

class Conversation(Base):
    __tablename__ = 'Conversation'
    id = Column(Integer, primary_key=True, autoincrement=True)
    character_id = Column(Integer, ForeignKey('Character.id'), nullable=False)
    message_user = Column(Text)
    message_npc = Column(Text)
    created_at = Column(DateTime, default=datetime.utcnow)

class Resumen(Base):
    __tablename__ = 'Resumen'
    id = Column(Integer, primary_key=True, autoincrement=True)
    character_id = Column(Integer, ForeignKey('Character.id'), unique=True)
    resumen_text = Column(Text)
    updated_at = Column(DateTime, default=datetime.utcnow, onupdate=datetime.utcnow)

class Folder(Base):
    __tablename__ = 'Folder'
    id = Column(Integer, primary_key=True, autoincrement=True)
    name = Column(String(255), unique=True) # Nombre de la carpeta (ej. "LoreMedieval")
    route = Column(String(500))            # Ruta interna (ej. "uploads/LoreMedieval")
    # parent_folder lo podemos quitar si no vas a hacer subcarpetas anidadas