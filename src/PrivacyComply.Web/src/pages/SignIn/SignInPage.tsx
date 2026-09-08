import { type FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuthentication } from '../../auth/useAuthentication'

export default function SignInPage() {
    const navigate = useNavigate()

    const {
        signIn,
        isSigningIn,
    } = useAuthentication()

    const [emailAddress, setEmailAddress] =
        useState('demo.clientadmin@privacycomply.local')

    const [password, setPassword] =
        useState('PrivacyComply-Demo-2026!')

    const [errorMessage, setErrorMessage] =
        useState<string | null>(null)

    async function handleSubmit(
        event: FormEvent<HTMLFormElement>,
    ) {
        event.preventDefault()
        setErrorMessage(null)

        try {
            const authenticationSession =
                await signIn({
                    emailAddress,
                    password,
                })

            const activeOrganisation =
                authenticationSession.activeOrganisation

            if (!activeOrganisation) {
                throw new Error(
                    'No active organisation is available for this account.',
                )
            }

            navigate(
                `/${activeOrganisation.organisationSlug}/dashboard`,
                {
                    replace: true,
                },
            )
        } catch (error) {
            setErrorMessage(
                error instanceof Error
                    ? error.message
                    : 'Unable to sign in.',
            )
        }
    }

    return (
        <main>
            <section>
                <h1>Sign in to PrivacyComply</h1>

                <form onSubmit={handleSubmit}>
                    <div>
                        <label htmlFor="emailAddress">
                            Email address
                        </label>

                        <input
                            id="emailAddress"
                            name="emailAddress"
                            type="email"
                            autoComplete="username"
                            value={emailAddress}
                            onChange={event =>
                                setEmailAddress(
                                    event.target.value,
                                )
                            }
                            disabled={isSigningIn}
                            required
                        />
                    </div>

                    <div>
                        <label htmlFor="password">
                            Password
                        </label>

                        <input
                            id="password"
                            name="password"
                            type="password"
                            autoComplete="current-password"
                            value={password}
                            onChange={event =>
                                setPassword(
                                    event.target.value,
                                )
                            }
                            disabled={isSigningIn}
                            required
                        />
                    </div>

                    {errorMessage && (
                        <p role="alert">
                            {errorMessage}
                        </p>
                    )}

                    <button
                        type="submit"
                        disabled={isSigningIn}
                    >
                        {isSigningIn
                            ? 'Signing in...'
                            : 'Sign in'}
                    </button>
                </form>
            </section>
        </main>
    )
}