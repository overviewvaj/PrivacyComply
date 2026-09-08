import type { SignInOrganisation } from './authentication'

export interface AuthenticationSession {
    userAccountId: string
    displayName: string
    isPlatformUser: boolean

    accessToken?: string
    tokenType?: string
    expiresDateTime?: string

    organisations: SignInOrganisation[]
    activeOrganisation: SignInOrganisation | null
}

export interface AuthenticationSessionResponse {
    isAuthenticated: boolean
    resultCode: string
    message: string | null

    userAccountId: string | null
    displayName: string | null
    isPlatformUser: boolean

    organisations: SignInOrganisation[]
}