from sqlalchemy import Column, Integer, String, DateTime, ForeignKey
from sqlalchemy.ext.declarative import declarative_base
from datetime import datetime

Base = declarative_base()

class Character(Base):
    __tablename__ = 'Character'
    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String)
    Age = Column(String)
    Description = Column(String)
    Epoca = Column(String)

class Conversation(Base):
    __tablename__ = 'Conversation'
    Id = Column(Integer, primary_key=True, autoincrement=True)
    CharacterId = Column(Integer, ForeignKey('Character.Id'))
    Historial = Column(String)
    Hora = Column(DateTime, default=datetime.utcnow)

class Resumen(Base):
    __tablename__ = 'Resumen'
    Id = Column(Integer, primary_key=True, autoincrement=True)
    CharacterId = Column(Integer, ForeignKey('Character.Id'))
    ResumenText = Column(String)
    Hora = Column(DateTime, default=datetime.utcnow)

class Folder(Base):
    __tablename__ = 'Folder'
    Id = Column(Integer, primary_key=True, autoincrement=True)
    Name = Column(String)
    Route = Column(String)
    ParentFolder = Column(Integer, default=0)