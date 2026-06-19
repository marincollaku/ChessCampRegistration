export const CHESS_LEVELS = [
  'Fillestar',
  'Elementar',
  'Mesatar',
  'Avancuar',
  'Ekspert',
] as const;

export interface Registration {
  id: number;
  kidFullName: string;
  kidAge: number;
  kidSchool: string;
  kidChessLevel: string;
  parentName: string;
  parentPhone: string;
  parentEmail: string;
  createdAt: string;
  updatedAt: string | null;
}

export interface RegistrationFormData {
  kidFullName: string;
  kidAge: number | '';
  kidSchool: string;
  kidChessLevel: string;
  parentName: string;
  parentPhone: string;
  parentEmail: string;
}

export const emptyForm: RegistrationFormData = {
  kidFullName: '',
  kidAge: '',
  kidSchool: '',
  kidChessLevel: 'Fillestar',
  parentName: '',
  parentPhone: '',
  parentEmail: '',
};
