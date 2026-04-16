from sqlalchemy.orm import Session
from schemas import Character, Conversation, Resumen, Folder


def get_character_by_id(db: Session, character_id: int):
    return db.query(Character).filter(Character.Id == character_id).first()


def get_characters(db: Session):
    return db.query(Character).all()


def add_character(db: Session, name: str, age: str, description: str, epoca: str):
    new_char = Character(Name=name, Age=age, Description=description, Epoca=epoca)
    db.add(new_char)
    db.commit()
    db.refresh(new_char)
    return new_char


def add_conversation(db: Session, character_id: int, historial: str):
    new_convo = Conversation(CharacterId=character_id, Historial=historial)
    db.add(new_convo)
    db.commit()
    return new_convo


def get_conversations(db: Session, character_id: int):
    return db.query(Conversation).filter(Conversation.CharacterId == character_id).all()


def add_resumen(db: Session, character_id: int, text: str):
    res = Resumen(CharacterId=character_id, ResumenText=text)
    db.add(res)
    db.commit()
    return res


def update_resumen(db: Session, character_id: int, new_text: str):
    res = db.query(Resumen).filter(Resumen.CharacterId == character_id).first()
    if res:
        res.ResumenText = new_text
        db.commit()
    return res


# ===========================
# GESTIÓN DE CARPETAS (SISTEMA DE ARCHIVOS)
# ===========================

def update_or_create_root_folder(db: Session, name: str, route: str):
    db_folder = db.query(Folder).filter(Folder.Id == 1).first()

    if db_folder:
        db_folder.Name = name
        db_folder.Route = route
        db.commit()
        db.refresh(db_folder)
        return db_folder
    else:
        new_folder = Folder(Id=1, Name=name, Route=route, ParentFolder=0)
        db.add(new_folder)
        db.commit()
        db.refresh(new_folder)
        return new_folder


def get_folder_by_id(db: Session, folder_id: int):
    return db.query(Folder).filter(Folder.Id == folder_id).first()
